using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;


namespace SnapshotUtilityForIBI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModel();
            this.InitializeTimer();
            this.lblVersion.Content = $"Ver.{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
        }

        ~MainWindow()
        {
            this.timerM.Stop();
        }

        private void InitializeTimer()
        {
            this.timerM.Interval = 100;
            this.timerM.Tick += TimerM_Tick;
            this.timerM.Start();
        }

        private string RemovalDevicePath = string.Empty;
        private string OriginalPath = "";
        public enum TARGETS
        {
            Lottrack = 0,
            Logs,
            E10info,
            Dose_Log,
            Backups,
            Config,
            Databases,
            HolderTracking
        }

        public enum FOLDER_RENAME
        {
            None = -1,
            Lottrack =0,
            Logs,
        }

        /// <summary>
        /// Backup用
        /// </summary>
        public static string[] P500Folders = { "ALog", "AReport", "bin", "Cycling", "EM", "HTracking", "Host", "MotionPrameter", "Perse", "STracking", "Stations", "TL", "TCAT", "Host", "User" };

        /// <summary>
        /// 実行中flag
        /// </summary>
        public bool executeFlag = false;

        /// <summary>
        /// Monitor Timer
        /// </summary>
        private Timer timerM = new Timer();

        private Stopwatch swRunTime = new Stopwatch();

        #region /// Event Method
        /// <summary>
        /// Execute backup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnTakeBuackup_Click(object sender, RoutedEventArgs e)
        {
            if (executeFlag) { return; }

            // Show message to prevent executing while running panel
            var result = System.Windows.MessageBox.Show("Please don't take backup while running panels on the tool. Select 'Cancel' if panels are running on the tool. Select 'OK' to continue.", "", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) { return; }

            // If process usage grater 50%, it does not execute.
            //var usage = this.GetCpuUsage();
            //if (usage > 50)
            //{
            //    System.Windows.MessageBox.Show("MemoryUsage > 50%. It can't execute.");
            //    return;
            //}

            //if (string.IsNullOrEmpty(this.cbToolNo.Text))
            //{
            //    System.Windows.MessageBox.Show("Please select Tool No");
            //    return;
            //}

            if (!string.IsNullOrEmpty(this.cbToolNo.Text))
            {
                this.OriginalPath = $@"\\{this.cbToolNo.Text}-control\c\P500";
            }

            swRunTime.Start();
            executeFlag = true;
            var vm = this.DataContext as ViewModel;
            try
            {
                await Task.Run(() =>
                {
                    // Check conditions.
                    if (vm.DestinationFolder == string.Empty)
                    {
                        System.Windows.MessageBox.Show("Destination folder name is empty");
                        return;
                    }
                    // Check USB is connected.
                    if (RemovalDevicePath == string.Empty || !Directory.Exists(RemovalDevicePath))
                    {
                        System.Windows.MessageBox.Show("Please confirm the destination path");
                        return;
                    }
                    // Folder Destination Path create
                    string destinationPath = RemovalDevicePath;
                    this.CheckAndCreateDirectory(destinationPath);

                    // Check duration
                    if (this.DpStartDate.Dispatcher.Invoke(() => { return this.DpStartDate.SelectedDate; }) == null || this.DpEndDate.Dispatcher.Invoke(() => { return this.DpEndDate.SelectedDate; }) == null)
                    {
                        System.Windows.MessageBox.Show("Please enter duration.");
                        return;
                    }

                    var startDate = (DateTime)this.DpStartDate.Dispatcher.Invoke(() => { return this.DpStartDate.SelectedDate; });
                    var endDate   = (DateTime)this.DpEndDate.Dispatcher.Invoke  (() => { return this.DpEndDate.SelectedDate;   });

                    if (startDate > endDate)
                    {
                        System.Windows.MessageBox.Show("Invalid Date. Please enter [StartDate] < [EndDate]");
                        return;
                    }
                    if (endDate == DateTime.Today)
                    {
                        if (System.Windows.MessageBox.Show("End date is today. Are you ok?", string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                    }

                    if(this.CheckFilesInTheFolder(destinationPath) || this.CheckDirectoriesInTheFolder(destinationPath))
                    {
                        var yesno = System.Windows.MessageBox.Show("Destination Folder is not empty. Do you want to recreate the folder ?","", MessageBoxButton.YesNo);
                        if(yesno == MessageBoxResult.Yes)
                        {
                            Directory.Delete(destinationPath, true);
                            this.CheckAndCreateDirectory(destinationPath);
                        }
                    }

                    List<TARGETS> archiveTargets = new List<TARGETS>();
                    if (vm.EnableLTF)
                    {
                        archiveTargets.Add(TARGETS.Lottrack);
                    }
                    if (vm.EnableLogs)
                    {
                        archiveTargets.Add(TARGETS.Logs);
                    }
                    if (vm.EnableE10Logs)
                    {
                        archiveTargets.Add(TARGETS.E10info);
                    }
                    if (vm.EnableDoseLog)
                    {
                        archiveTargets.Add(TARGETS.Dose_Log);
                    }
                    if (vm.EnableP500Backup)
                    {
                        archiveTargets.Add(TARGETS.Backups);
                    }
                    if (vm.EnableConfig)
                    {
                        archiveTargets.Add(TARGETS.Config);
                    }
                    if (vm.EnableDatabase)
                    {
                        archiveTargets.Add(TARGETS.Databases);
                    }
                    if (vm.EnableHolderTracking)
                    {
                        archiveTargets.Add(TARGETS.HolderTracking);
                    }

                    // Compress and Archvie 
                    for (int i = 0; i < archiveTargets.Count(); i++)
                    {
                        TARGETS target     = archiveTargets[i];
                        string destination = System.IO.Path.Combine(destinationPath, target.ToString());
                        string origin      = System.IO.Path.Combine(OriginalPath, target.ToString());

                        double unitProgress = 100 / archiveTargets.Count();

                        switch (target)
                        {
                            case TARGETS.Lottrack:
                                // Compress and archive from Completed folder.
                                if(!vm.LTFArchiveIsEnabled && vm.EnableCompressing)
                                {
                                    unitProgress /= 3;
                                }
                                else if(!vm.LTFArchiveIsEnabled && !vm.EnableCompressing)
                                {
                                    unitProgress /= 2;
                                }

                                this.CopyDirectory(new DirectoryInfo(origin + "\\Archive"), startDate, endDate, destination, true, vm, unitProgress, FOLDER_RENAME.Lottrack);
                                if (!vm.LTFArchiveIsEnabled)
                                {
                                    this.CopyDirectory(new DirectoryInfo(origin + "\\Completed"), startDate, endDate, destination + "\\Completed", true, vm, unitProgress,FOLDER_RENAME.None);
                                    if (vm.EnableCompressing)
                                    {
                                        try
                                        {
                                            //string serverUNC = @"\\10.0.0.1";
                                            //string ServerId = "SN545-CONTROL\\User";
                                            //string ServerPass = "user";
                                            //NetworkCredential nc = new NetworkCredential(ServerId, ServerPass);
                                            //using (new ConnectToSharerdFolder(serverUNC, nc))
                                            //{
                                            //    ProcessStartInfo psi = new ProcessStartInfo();
                                            //    psi.FileName = ORIGINALPATH + "\\LTFArchiveByDate\\bin\\Release\\LTFArchiveByDateFromBackupTool.exe";
                                            //    string start = startDate.ToString("yyyyMMdd");
                                            //    string end = endDate.ToString("yyyyMMdd");
                                            //    psi.Arguments = string.Format("{0} {1}", start, end);
                                            //    psi.Verb = "RunAs";
                                            //    var p = Process.Start(psi);
                                            //    p.WaitForExit();
                                            //    int ret = p.ExitCode;
                                            //    if (ret < 0) { throw new ExternalException(string.Format("LTF archive failed {0}", ret)); }
                                            //}
                                            this.CompressAndArchiveByDate(destination, startDate, endDate,vm ,unitProgress ,isLtf:true);
                                            this.CheckAndDeleteDirectory(destination + "\\Completed",false);

                                        }
                                        catch (Exception ex)
                                        {
                                            System.Windows.MessageBox.Show(ex.Message);
                                            return;
                                        }
                                    }
                                }
                                break;
                            case TARGETS.Logs:
                                this.CheckAndCreateDirectory(destination);
                                // Log Archive
                                this.CopyDirectory(new DirectoryInfo(origin), startDate, endDate, destination, true, vm, unitProgress, FOLDER_RENAME.Logs);
                                if (vm.EnableCompressing)
                                {
                                    this.updateFileAttributes(new DirectoryInfo(destination));
                                    foreach (var directroy in Directory.GetDirectories(destination + "\\"))
                                    {
                                        foreach (var childDir in Directory.GetDirectories(directroy))
                                        {
                                            vm.CurrentDeal = $"Compress : {childDir}";
                                            this.CheckAndCompressDirectory(childDir);
                                        }
                                    }
                                }
                                break;
                            case TARGETS.E10info:
                                this.CheckAndCreateDirectory(destination);
                                this.CopyDirectory(new DirectoryInfo(origin), startDate, endDate, destination, true, vm, unitProgress,FOLDER_RENAME.None);
                                break;
                            case TARGETS.Dose_Log:
                                destination = destination.Replace("Dose_Log", "DoseLog");
                                this.CheckAndCreateDirectory(destination);
                                this.CopyDirectory(new DirectoryInfo(origin), startDate, endDate, destination, true, vm, unitProgress,FOLDER_RENAME.None);
                                break;
                            case TARGETS.Config:
                                var dirConfig = new DirectoryInfo(origin);
                                string date = DateTime.Today.ToString("yyyyMMdd");
                                var dstFolderConfig = System.IO.Path.GetFileName(destination) + "_" + date;
                                destination = destination.Replace("Config", "ConfigFiles");
                                this.CheckAndCreateDirectory(destination);
                                var dstPathConfig = System.IO.Path.Combine(destination, dstFolderConfig);
                                this.CopyDirectory(dirConfig, DateTime.MinValue, DateTime.MinValue, dstPathConfig, isRecursive:false, vm, unitProgress,FOLDER_RENAME.None);

                                if (vm.EnableCompressing && !File.Exists(dstPathConfig + ".zip"))
                                {
                                    // change folder property
                                    this.updateFileAttributes(new DirectoryInfo(destination));
                                    vm.CurrentDeal = "Compress : Config Folder";
                                    this.CheckAndCompressDirectory(dstPathConfig);
                                }
                                break;
                            case TARGETS.Databases:
                                this.CheckAndCreateDirectory(destination);
                                DirectoryInfo dirDatabases = new DirectoryInfo(origin);
                                this.CopyDirectory(dirDatabases, DateTime.MinValue, DateTime.MinValue, destination,true, vm, unitProgress,FOLDER_RENAME.None);
                                //if (vm.EnableCompressing && !File.Exists(destination + ".zip"))
                                //{
                                //    this.updateFileAttributes(new DirectoryInfo(destination));
                                //    vm.CurrentDeal = "Compress Database Folder";
                                //    System.IO.Compression.ZipFile.CreateFromDirectory(destination, destination + ".zip", System.IO.Compression.CompressionLevel.Optimal, false, System.Text.Encoding.GetEncoding("shift_jis"));
                                //    Directory.Delete(destination, true);
                                //}
                                break;
                            case TARGETS.HolderTracking:
                                this.CheckAndCreateDirectory(destination);
                                this.CopyDirectory(new DirectoryInfo(origin), DateTime.MinValue, DateTime.MinValue, destination, true, vm, unitProgress,FOLDER_RENAME.None);
                                break;
                            case TARGETS.Backups:
                                string backup = "P500_" + DateTime.Today.ToString("yyyyMMdd");
                                string P500folder = destination + "\\" + backup;
                                
                                this.CheckAndCreateDirectory(destination);
                                this.CheckAndCreateDirectory(P500folder);

                                DirectoryInfo dirP500 = new DirectoryInfo(OriginalPath);
                                var directries        = dirP500.GetDirectories();
                                unitProgress /= directries.Count();
                                foreach (DirectoryInfo dir in directries)
                                {
                                    if (P500Folders.Contains(dir.Name))
                                    {
                                        this.CopyDirectory(dir, DateTime.MinValue, DateTime.MinValue, P500folder + "\\" + dir.Name, true, vm, unitProgress,FOLDER_RENAME.None);
                                    }
                                    else
                                    {
                                        vm.Progress += unitProgress;
                                    }
                                }
                                if (vm.EnableCompressing && !File.Exists(P500folder + ".zip"))
                                {
                                    vm.CurrentDeal = "Compress : P500 Backup";
                                    this.CheckAndCompressDirectory(P500folder);
                                }
                                break;
                            default:
                                break;
                        }
                        this.CheckAndDeleteDirectory(destination, true);
                    }
                });
                swRunTime.Stop();
                System.Windows.MessageBox.Show("Finished!");
            }
            catch (Exception err)
            {
                System.Windows.MessageBox.Show(err.Message + "\r\n" + err.StackTrace, "Catch Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (this.swRunTime.IsRunning)
            {
                swRunTime.Stop();
                swRunTime.Reset();
            }
            vm.Progress = 0;
            vm.CurrentDeal = "";
            if (executeFlag) { executeFlag = false; }
        }

        /// <summary>
        /// Select folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbo = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
            };
            var result = fbo.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel) { return; }

            // Set file Path
            this.tbDestinationFolderName.Text = System.IO.Path.GetFileName(fbo.SelectedPath);
            this.RemovalDevicePath = fbo.SelectedPath;
            this.gbLogTypes.IsEnabled = true;
            this.gbOtherOption.IsEnabled = true;
            this.btnTakeBackup.IsEnabled = true;
        }


        #region // CheckBox Event

        private void ChkLogs_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.gbDuration.IsEnabled)
            {
                this.gbDuration.IsEnabled = true;
            }
        }

        private void ChkLogs_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.gbDuration.IsEnabled)
            {
                if (!(bool)this.ChkE10Logs.IsChecked && !(bool)this.ChkDoseLog.IsChecked && !(bool)this.ChkLTF.IsChecked)
                {
                    this.gbDuration.IsEnabled = false;
                }
            }
        }

        private void ChkE10Logs_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.gbDuration.IsEnabled)
            {
                this.gbDuration.IsEnabled = true;
            }
        }

        private void ChkE10Logs_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.gbDuration.IsEnabled)
            {
                if (!(bool)this.ChkLogs.IsChecked && !(bool)this.ChkDoseLog.IsChecked && !(bool)this.ChkLTF.IsChecked)
                {
                    this.gbDuration.IsEnabled = false;
                }
            }
        }

        private void ChkLTF_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.gbDuration.IsEnabled)
            {
                this.gbDuration.IsEnabled = true;
            }
        }

        private void ChkLTF_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.gbDuration.IsEnabled)
            {
                if (!(bool)this.ChkE10Logs.IsChecked && !(bool)this.ChkDoseLog.IsChecked && !(bool)this.ChkLogs.IsChecked)
                {
                    this.gbDuration.IsEnabled = false;
                }
            }
        }

        private void ChkDoseLog_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.gbDuration.IsEnabled)
            {
                this.gbDuration.IsEnabled = true;
            }
        }

        private void ChkDoseLog_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.gbDuration.IsEnabled)
            {
                if (!(bool)this.ChkE10Logs.IsChecked && !(bool)this.ChkLogs.IsChecked && !(bool)this.ChkLTF.IsChecked)
                {
                    this.gbDuration.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Update Read only FileAttributes to false
        /// </summary>
        /// <param name="dinfo"></param>
        private void updateFileAttributes(DirectoryInfo dinfo)
        {
            if ((dinfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                dinfo.Attributes &= ~FileAttributes.ReadOnly;
            }

            foreach (FileInfo fInfo in dinfo.GetFiles())
            {
                if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    fInfo.Attributes &= FileAttributes.ReadOnly;
                }
            }
            foreach (DirectoryInfo subDInfo in dinfo.GetDirectories())
            {
                this.updateFileAttributes(subDInfo);
            }
        }

        /// <summary>
        /// Timer Tick event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerM_Tick(object sender, EventArgs e)
        {
            if (this.executeFlag)
            {
                this.IsEnabled = false;
                this.lblRunTime.Content = $"{(int)this.swRunTime.Elapsed.TotalSeconds} sec";
            }
            else
            {
                this.IsEnabled = true;
                this.lblRunTime.Content = string.Empty;
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (executeFlag)
            {
                var yesno = System.Windows.MessageBox.Show("Do you want to stop copying ?", "", MessageBoxButton.YesNo);
                if (yesno == MessageBoxResult.No) { e.Cancel = true; return; }
            }
        }
        #endregion

        /// <summary>
        /// Archive and compress for LTF
        /// </summary>
        /// <remarks>should use P500 isn't production</remarks>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void CompressAndArchiveByDate(string path, DateTime startDate, DateTime endDate,ViewModel vm, double unitProgress, bool isLtf)
        {
            try
            {
                var targetFolder = string.Empty;
                string OriginPath = System.IO.Path.Combine(path, "Completed");

                var ltfFiles   = Directory.GetFiles(OriginPath, "*.xml");
                var totalDates = (endDate - startDate).TotalDays;
                var fileList   = new List<string>();
                unitProgress /= totalDates;

                for (int i = 0; i <= totalDates; i++)
                {
                    var targetDate = startDate.AddDays(i);
                    // Create folder
                    string folderName = targetDate.Year.ToString();
                    folderName += $"{targetDate.Month:D2}";
                    folderName += $"{targetDate.Day:D2}";
                    folderName = folderName.Substring(2);

                    // TargetPath を変える
                    if (isLtf)
                    {
                        if (!Regex.IsMatch(folderName, "\\d{6}")) { continue; }
                        int year, month, date;
                        string fileName = System.IO.Path.GetFileName(folderName);
                        year = Convert.ToInt32("20" + fileName.Substring(0, 2));
                        month = Convert.ToInt32(fileName.Substring(2, 2));
                        date = Convert.ToInt32(fileName.Substring(4, 2));
                        var fileDate = new DateTime(year, month, date);
                        targetFolder = this.GetMonthFolderNameFromDate(fileDate);
                    }

                    string targetFolderPath = System.IO.Path.Combine(path, targetFolder);
                    string target     = targetFolderPath + "\\" + folderName;
                    vm.CurrentDeal    = "File Moving : " + target;

                    this.CheckAndCreateDirectory(targetFolderPath);
                    this.CheckAndCreateDirectory(target);

                    DateTime lastWriteTime = DateTime.Now;
                    foreach (var item in ltfFiles)
                    {
                        lastWriteTime = File.GetLastWriteTime(item);
                        try
                        {
                            if (lastWriteTime.Year != targetDate.Year || lastWriteTime.Date != targetDate.Date) { continue; }

                            string file = System.IO.Path.GetFileName(item);
                            this.CheckAndMoveFile(item, target + "\\" + file);
                            fileList.Add(target + "\\" + file);
                        }
                        catch
                        {

                        }
                    }
                    string zipFile = target + ".zip";
                    vm.CurrentDeal = "Compressing : " + System.IO.Path.GetFileName(zipFile);

                    if (Directory.GetFiles(target).Count() != 0)
                    {
                        if (!File.Exists(zipFile))
                        {
                            System.IO.Compression.ZipFile.CreateFromDirectory(target, zipFile, System.IO.Compression.CompressionLevel.Optimal, false);
                        }
                        else
                        {
                            using (ZipArchive a = ZipFile.Open(zipFile, ZipArchiveMode.Update))
                            {
                                foreach (var item in fileList)
                                {
                                    if (File.Exists(item)) { continue; }
                                    ZipArchiveEntry e = a.CreateEntryFromFile(item, System.IO.Path.GetFileName(item));
                                }
                            }
                        }
                    }
                    this.CheckAndDeleteDirectory(target, false);
                    vm.Progress += unitProgress;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Folder Copy for specific term
        /// </summary>
        /// <param name="di">original directory info</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="targetFolder"> Destination Path</param>
        /// <param name="isRecursive"></param>
        /// <param name="unitProgress"></param>
        private void CopyDirectory(DirectoryInfo di, DateTime startDate, DateTime endDate, string targetFolder, bool isRecursive, ViewModel vm, double unitProgress, FOLDER_RENAME rename)
        {
            var files = di.GetFiles();
            var subDirectories = di.GetDirectories();

            if (files.Count() + subDirectories.Count() == 0) { vm.Progress += unitProgress; return; }

            unitProgress /= isRecursive ? (files.Count() + subDirectories.Count()) : files.Count();


            foreach (FileInfo fi in files)
            {

                // SetTargetFolder
                var tempTarget = targetFolder;
                if (rename != FOLDER_RENAME.None)
                {
                    int year = 0, month = 0, date = 0;
                    if (rename == FOLDER_RENAME.Lottrack)
                    {
                        if (!Regex.IsMatch(fi.Name, "\\d{6}")) { vm.Progress += unitProgress; continue; }
                        string fileName = System.IO.Path.GetFileName(fi.Name);
                        if (!int.TryParse("20" + fileName.Substring(0, 2), out _)) { vm.Progress += unitProgress; continue; }
                        if (startDate == DateTime.MinValue || endDate == DateTime.MinValue) { vm.Progress += unitProgress; continue; }

                        var fileDate = new DateTime(Convert.ToInt32("20" + fi.Name.Substring(0, 2)), Convert.ToInt32(fi.Name.Substring(2, 2)), Convert.ToInt32(fi.Name.Substring(4, 2)));
                        if (fileDate < startDate || fileDate > endDate)
                        {
                            vm.Progress += unitProgress;
                            continue;
                        }

                        year            = Convert.ToInt32("20" + fileName.Substring(0, 2));
                        month           = Convert.ToInt32(fileName.Substring(2, 2));
                        date            = Convert.ToInt32(fileName.Substring(4, 2));
                        tempTarget      = tempTarget.Replace("Lottrack", "LottrackFiles");
                        if (year > 0 && month > 0 && date > 0)
                        {
                            tempTarget = Path.Combine(tempTarget, this.GetMonthFolderNameFromDate(new DateTime(year, month, date)));
                        }
                    }
                    else if (rename == FOLDER_RENAME.Logs)
                    {
                        var dirName = Path.GetFileName(fi.FullName);
                        if (!Regex.IsMatch(dirName, "\\d{8}")) { vm.Progress += unitProgress; continue; }
                        dirName = dirName.Substring(dirName.IndexOf('.') + 1, 8);
                        if (!int.TryParse(dirName.Substring(0, 4), out _)) { vm.Progress += unitProgress; continue; }
                        if (startDate == DateTime.MinValue || endDate == DateTime.MinValue) { vm.Progress += unitProgress; continue; }
                        if (fi.LastWriteTime.Date < startDate || fi.LastWriteTime.Date > endDate)
                        {
                            vm.Progress += unitProgress;
                            continue;
                        }

                        year    = Convert.ToInt32(dirName.Substring(0, 4));
                        month   = Convert.ToInt32(dirName.Substring(4, 2));
                        date    = Convert.ToInt32(dirName.Substring(6, 2));
                        if (year > 0 && month > 0 && date > 0)
                        {
                            var fileDate = new DateTime(year, month, date);
                            string dateNamedFolder = this.GetMonthFolderNameFromDate(fileDate);

                            string te = tempTarget;
                            tempTarget = System.IO.Path.Combine(te, dateNamedFolder);
                        }
                    }
                }
                string fiName = System.IO.Path.Combine(tempTarget, fi.Name);
                this.CheckAndCreateDirectory(tempTarget);

                vm.CurrentDeal = "File Copy : " + fi.Name;
                if (!File.Exists(fiName))
                {
                    File.Copy(fi.FullName, fiName);
                }
                vm.Progress += unitProgress;
            }

            if (!isRecursive) { return; }

            // Recursive
            foreach (DirectoryInfo sub in subDirectories)
            {
                if (startDate != DateTime.MinValue && endDate != DateTime.MinValue)
                {
                    if (sub.LastWriteTime.Date < startDate || sub.LastWriteTime.Date > endDate) { vm.Progress += unitProgress; continue; }
                }
                string subFolder = targetFolder + "\\" + sub.Name;
                this.CopyDirectory(sub, startDate, endDate, subFolder,isRecursive, vm, unitProgress,rename);
            }
        }

        /// <summary>
        /// Get Month_Year Name from date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetMonthFolderNameFromDate(DateTime date)
        {
            string dstFolder = string.Empty;
            switch (date.Month)
            {
                case 1:
                    dstFolder = "Jan_";
                    break;
                case 2:
                    dstFolder = "Feb_";
                    break;
                case 3:
                    dstFolder = "Mar_";
                    break;
                case 4:
                    dstFolder = "Apr_";
                    break;
                case 5:
                    dstFolder = "May_";
                    break;
                case 6:
                    dstFolder = "Jun_";
                    break;
                case 7:
                    dstFolder = "Jul_";
                    break;
                case 8:
                    dstFolder = "Aug_";
                    break;
                case 9:
                    dstFolder = "Sep_";
                    break;
                case 10:
                    dstFolder = "Oct_";
                    break;
                case 11:
                    dstFolder = "Nov_";
                    break;
                case 12:
                    dstFolder = "Dec_";
                    break;
            }
            return dstFolder + date.Year.ToString();
        }

        /// <summary>
        /// Check whether there are files in the folder
        /// </summary>
        /// <param name="folderName"></param>
        private bool CheckFilesInTheFolder(string folderName)
        {
            bool result = false;
            int counts = Directory.GetFiles(folderName).Count();
            if (counts > 0) { return true; }

            foreach (string item in Directory.GetDirectories(folderName))
            {
                result = this.CheckFilesInTheFolder(item);
                if (result) { break; }
            }
            return result;
        }

        /// <summary>
        /// Check whether there are folders in the folder
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private bool CheckDirectoriesInTheFolder(string folderName)
        {
            bool result = false;
            int counts = Directory.GetDirectories(folderName).Count();
            if (counts > 0) { return true; }

            foreach (string item in Directory.GetDirectories(folderName))
            {
                result = this.CheckDirectoriesInTheFolder(item);
                if (result) { break; }
            }
            return result;
        }

        /// <summary>
        /// Get value from Performance Counter
        /// </summary>
        private float GetCpuUsage()
        {
            string machineName = ".";
            string categoryName = "Processor";
            string counterName = "% Processor Time";
            string instanceName = "_Total";
            System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter(categoryName, counterName, instanceName, machineName);
            return pc.NextValue();
        }

        #region /// File Operation
        private void CheckAndCreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        private void CheckAndRenameDirectory(string originalPath, string renamePath)
        {
            if (!Directory.Exists(renamePath))
            {
                Directory.Move(originalPath, renamePath);
            }
        }

        private void CheckAndDeleteDirectory(string directory, bool checkHaveFiles)
        {
            if (Directory.Exists(directory))
            {
                if (checkHaveFiles)
                {
                    if (!this.CheckFilesInTheFolder(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                else
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private void CheckAndCompressDirectory(string directroy)
        {
            this.CheckAndDeleteFile(directroy + ".zip");
            System.IO.Compression.ZipFile.CreateFromDirectory(directroy, directroy + ".zip", System.IO.Compression.CompressionLevel.Optimal, false);
            this.CheckAndDeleteDirectory(directroy, false);
        }

        private void CheckAndDeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        private void CheckAndMoveFile(string org, string dst)
        {
            this.CheckAndDeleteFile(dst);
            File.Move(org, dst);
        }
        #endregion

        /// <summary>
        /// Set availability of Group Box
        /// </summary>
        /// <param name="isEnable">true: enable , false: disable</param>
        private void EnableGroupBox(bool isEnable)
        {
            this.gbLogTypes.IsEnabled = isEnable;
            this.gbDuration.IsEnabled = isEnable;
            this.gbOtherOption.IsEnabled = isEnable;
        }
    }
    #endregion

    /// <summary>
    /// View Model Class
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        private double _progress;
        private string currentDeal;
        private string destinationFolder;
        private bool enableLogs;
        private bool enableLTF;
        private bool enableE10Logs;
        private bool enableDoseLog;
        private bool enableConfig;
        private bool enableP500Backup;
        private bool enableHolderTracking;
        private bool enableDatabase;
        private bool enableCompressing;
        private bool ltfArchiveIsEnabled;
        private string txtRunTime;

        /// <summary>
        /// Progress Bar Content
        /// </summary>
        public double Progress
        {
            get { return this._progress; }
            set
            {
                this._progress = value;
                this.NotifyPropertyChanged(nameof(this.Progress));
            }
        }

        /// <summary>
        /// Label Current Deal Content
        /// </summary>
        public string CurrentDeal
        {
            get { return this.currentDeal; }
            set
            {
                this.currentDeal = value;
                this.NotifyPropertyChanged(nameof(this.CurrentDeal));
            }
        }

        /// <summary>
        /// TextBox Destination Folder
        /// </summary>
        public string DestinationFolder
        {
            get { return this.destinationFolder; }
            set
            {
                this.destinationFolder = value;
                this.NotifyPropertyChanged(nameof(this.DestinationFolder));
            }
        }
        public bool EnableLogs
        {
            get { return this.enableLogs; }
            set
            {
                this.enableLogs = value;
                this.NotifyPropertyChanged(nameof(this.EnableLogs));
            }
        }
        public bool EnableLTF
        {
            get { return this.enableLTF; }
            set
            {
                this.enableLTF = value;
                this.NotifyPropertyChanged(nameof(this.EnableLTF));
            }
        }
        public bool EnableE10Logs
        {
            get { return this.enableE10Logs; }
            set
            {
                this.enableE10Logs = value;
                this.NotifyPropertyChanged(nameof(this.EnableE10Logs));
            }
        }
        public bool EnableDoseLog
        {
            get { return this.enableDoseLog; }
            set
            {
                this.enableDoseLog = value;
                this.NotifyPropertyChanged(nameof(this.EnableDoseLog));
            }
        }
        public bool EnableConfig
        {
            get { return this.enableConfig; }
            set
            {
                this.enableConfig = value;
                this.NotifyPropertyChanged(nameof(this.EnableConfig));
            }
        }
        public bool EnableDatabase
        {
            get { return this.enableDatabase; }
            set
            {
                this.enableDatabase = value;
                this.NotifyPropertyChanged(nameof(this.EnableDatabase));
            }
        }
        public bool EnableP500Backup
        {
            get { return this.enableP500Backup; }
            set
            {
                this.enableP500Backup = value;
                this.NotifyPropertyChanged(nameof(this.EnableP500Backup));
            }
        }
        public bool EnableHolderTracking
        {
            get { return this.enableHolderTracking; }
            set
            {
                this.enableHolderTracking = value;
                this.NotifyPropertyChanged(nameof(this.EnableHolderTracking));
            }
        }

        public bool EnableCompressing
        {
            get { return this.enableCompressing; }
            set
            {
                this.enableCompressing = value;
                this.NotifyPropertyChanged(nameof(this.EnableCompressing));
            }
        }
        public bool LTFArchiveIsEnabled
        {
            get { return this.ltfArchiveIsEnabled; }
            set
            {
                this.ltfArchiveIsEnabled = value;
                this.NotifyPropertyChanged(nameof(this.LTFArchiveIsEnabled));
            }
        }
        public string TxtRunTime 
        {
            get { return this.txtRunTime; }
            set
            {
                this.txtRunTime = value;
                this.NotifyPropertyChanged(nameof(this.txtRunTime));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ConnectToSharerdFolder : IDisposable
    {
        readonly string networkName;

        public ConnectToSharerdFolder(string name, NetworkCredential networkCredential)
        {
            networkName = name;
            var netResource = new NETRESOURCE
            {
                dwScope = ResourceScope.Connected,
                dwType = ResourceType.Disk,
                dwDisyplayType = ResourceDisplaytype.Generic,
                dwUsage = 0,
                IpRemoteName = networkName
            };

            var userName = string.IsNullOrEmpty(networkCredential.Domain) ? networkCredential.UserName : string.Format(@"{0}\{1}", networkCredential.Domain, networkCredential.UserName);
            var result = WNetAddConnection2(ref netResource, networkCredential.Password, userName, 0);
            if (result != 0)
            {
                throw new Win32Exception(result, "Error connecting to remote share");
            }
        }


        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string IpName, Int32 dwFlags, bool Force);
        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE IpNetResource, string IpPassword, string IpUsername, Int32 dwFlags);

        ~ConnectToSharerdFolder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(networkName, 0, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NETRESOURCE
        {
            public ResourceScope dwScope;
            public ResourceType dwType;
            public ResourceDisplaytype dwDisyplayType;
            public int dwUsage;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string IpLocalName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string IpRemoteName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string IpComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string IpProvider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GrobalNetwork,
            Remembered,
            Recent,
            Context
        };
        public enum ResourceType : int
        {
            Any          = 0,
            Disk         = 1,
            Print        = 2,
            Reserved     = 8,
            unknown      = -1
        }

        public enum ResourceDisplaytype : int
        {
            Generic      = 0x0,
            Domain       = 0x01,
            Server       = 0x02,
            Share        = 0x03,
            File         = 0x04,
            Group        = 0x05,
            Network      = 0x06,
            Root         = 0x07,
            Shareadmin   = 0x08,
            Directory    = 0x09,
            Tree         = 0x0a,
            Ndscontainer = 0x0b
        }
    }
 }

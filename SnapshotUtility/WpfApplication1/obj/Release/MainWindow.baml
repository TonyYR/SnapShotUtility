   M S B A M L   `   `   `  ���� O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem   SnapshotUtilityForIBI&    SnapshotUtilityForIBI.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     9System&clr-namespace:System;assembly=mscorlib     -     � Title$  Snapshot Utility�� � Icon$ LOGO.ico��$	��550��$	��450�� � 
ResizeMode$ NoResize=����	#FFE5E5E5.��� $��0,0,0,0q�.+��� $
��Left=�$	��510��$	��Top=�$	��440��$��5�����	#FFE5E5E5.@��� $��
Log Backup��.��� ���	#FFE5E5E5.+��� -    ��Name$ btnTakeBackup��$��Take Backup��$
��Left=�$��20��$��125,394,0,0q�$	��Top=�$	��180���� $��Destination Folder Name��$
��Left=�$��27��$��5,64,0,0q�$	��Top=�$	��155���� -   $��tbDestinationFolderName��$
��Left=�$��25��$��
150,64,0,0q� �� TextWrapping$
 Wrap=�$	��Top=�$	��215������ *DestinationFolder+�� $��Backup Log files��$
��Left=�$��27��$��9,0,0,0q�$	��Top=�$	��108��� -   $
��PBar��$
��Left=�$��15��$��
45,424,0,0q�$	��Top=�$	��350�� � Maximum$	 100Q� � Minimum$ 0Q� � Value �� *
Progress+�� -   $��BtnSelectFolder��$	��...��$
��Left=�$��25��$��
370,64,0,0q�$	��Top=�$��28���� -    ��Name$ 
gbLogTypes��$��Select type of log��$
��Left=�$��85��$��
10,100,0,0q�$	��Top=�$	��393��.���� .+��� 	 �� Orientation$	 
Horizontal=�.+��� -   
 ��Name$
 ChkLogs��$
��Logs��$
��Left=�$��21��$	��Top=�$��89��$��	10,10,0,0q� d� 	IsChecked �� *
EnableLogs+�� -   $
 
ChkE10Logs��$��E10Logs��$��Center=�$��21��$	��Top=�$��89��$��	50,10,0,0q� �� *EnableE10Logs+�� $	 
Horizontal=�.+��� -	   $
 ChkLTF��$	��LTF��$
��Left=�$��21��$	��Top=�$��89��$��	10,10,0,0q� �� *	EnableLTF+�� -
   $
 
ChkDoseLog��$��DoseLog��$��Center=�$��21��$	��Top=�$��89��$��	50,10,0,0q� �� *EnableDoseLog+�� -   $ 
gbDuration��$��Select Duration��$
��Left=�$��95��$��
10,185,0,0q�$	��Top=�$	��393��.���� .+��� $	 
Horizontal=�.+��� $��
Start Date��$��27��$	��100��$	��Top=�$
��Left=�$��	10,10,0,0q�(  "System.Windows.Controls.DatePicker  -   $��DpStartDate��$
��Left=�$��26��$	��Top=�$	��115��$��3��$��	50,10,0,0q�   SelectedDate ��  �� Today �� Source# Z  �� Mode$ OneTime=��� $	 
Horizontal=�.+��� $��End Date��$��27��$	��100��$��Bottom=�$
��Left=�$��2��$��2��$��10,0,0,0q�  -   $��	DpEndDate��$
��Left=�$��26��$	��Top=�$	��115��$��3��$��50,0,0,0q� �� # Z $ OneTime=��� -   $��gbOtherOption��$ ��Select other backup option��$
��Left=�$��80��$��
10,285,0,0q�$	��Top=�$	��398��.���� $��	0,0,0,-13q�.+��� $	 
Horizontal=�.+��� -   $��	ChkConfig��$��Config��$
��Left=�$��21��$	��Top=�$��89��$��3��$��	10,10,0,0q� �� *EnableConfig+�� -   $��ChkDataBase��$��Database��$
��Left=�$��21��$	��Top=�$��89��$��2��$��	50,10,0,0q� �� *EnableDatabase+�� $	 
Horizontal=�.+��� -   $��ChkP500_Backup��$��P500 Backup��$
��Left=�$��21��$	��Top=�$��89��$��	10,10,0,0q� �� *EnableP500Backup+�� -   $��ChkHolderTracking��$��HolderTracking��$
��Left=�$��21��$	��Top=�$	��104��$��2��$��	50,10,0,0q� �� *EnableHolderTracking+�� -   $��lblProgress��$��Right=�$��32��$��
0,444,10,0q�$	��Top=�$	��414������ *CurrentDeal+�� -   $��ChkCompress��$��Enable Compress��$
��Left=�$��20��$��	9,370,0,0q�$	��Top=�$	��130�� �� *EnableCompressing+�� -   $��ChkLTFArchiveIsEnabled��$)��#LTF : copy from Archive folder only��$
��Left=�$��20��$��140,370,0,0q�$	��Top=�$	��217�� �� *LTFArchiveIsEnabled+�� -   $��lbToolNo��$��Tool No��$
��Left=�$��25��$��	10,30,0,0q�$	��Top=�$	��125���� -   $��cbToolNo��$
��Left=�$��20��$��
150,30,0,0q�$	��Top=�$��80��.@��� .��SN517�� .��SN520�� .��SN521�� .��SN522�� .��SN523�� .��SN530�� .��SN531�� .��SN533�� .��SN534�� .��SN541�� .��SN545�� -   $��
lblVersion��$��label��$
��Left=�$��23��$��	360,0,0,0q�$	��Top=�$��60��$��9(�$��Black"��� -   $��
lblRunTime��$
��Left=�$��24��$��325,392,0,0q�$	��Top=�$��93������ *
txtRunTime+
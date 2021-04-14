#region License
/*Данный код опубликован под лицензией Creative Commons Attribution-NonСommercial-ShareAlike.
Разрешено использовать, распространять, изменять и брать данный код за основу для производных 
в некоммерческих целях, при условии указания авторства и если производные лицензируются на тех же условиях.
Код поставляется "как есть". Автор не несет ответственности за возможные последствия использования.
Зуев Александр, 2021, все права защищены.
This code is listed under the Creative Commons Attribution-NonСommercial-ShareAlike license.
You may use, redistribute, remix, tweak, and build upon this work non-commercially,
as long as you credit the author by linking back and license your new creations under the same terms.
This code is provided 'as is'. Author disclaims any implied warranty.
Zuev Aleksandr, 2021, all rigths reserved.*/
#endregion
#region usings
using System;
using System.Diagnostics;
using System.IO;
#endregion

namespace IngradParametrisation
{
    public static class SettingsUtils
    { public const string configFileName = "IngdConfiguration.txt";

        public static string CheckOrCreateSettings()
        {
            Debug.WriteLine("Start read settings");
            string appdataFolder =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configPath = Path.Combine(appdataFolder, "bim-starter", "config.ini");
            Debug.WriteLine("Check central config file: " + configPath);
            string serverSettingsPath = "";
            if (File.Exists(configPath))
            {
                serverSettingsPath = File.ReadAllLines(configPath)[0];
                Debug.WriteLine("Path to shared folder: " + serverSettingsPath);
            }
            else
            {
                string sourceTxtFile = getLocalConfigFile();
                Debug.WriteLine("No shared folder, use local file: " + sourceTxtFile);
                return sourceTxtFile;
            }
            string ingdConfigFile = Path.Combine(serverSettingsPath, configFileName);
            Debug.WriteLine("Path to shared config file: " + ingdConfigFile);
            if (!File.Exists(ingdConfigFile))
            {
                Debug.WriteLine("Shared config file not found!");
                string sourceTxtFile = getLocalConfigFile();
                try
                {
                    File.Copy(sourceTxtFile, ingdConfigFile);
                    Debug.WriteLine("New shared config file created by local: " + sourceTxtFile);
                }
                catch(Exception ex)
                {
                    string msg = "Не удалось скопировать " + sourceTxtFile + " в " + ingdConfigFile + ": " + ex.Message;
                    Debug.WriteLine(msg);
                    throw new Exception(msg);
                }
            }
            Debug.WriteLine("Final ingd config file: " + ingdConfigFile);
            return ingdConfigFile;
        }

        private static string getLocalConfigFile()
        {
            string assemblyFolder = Path.GetDirectoryName(App.assemblyPath);
            string sourceTxtFile = Path.Combine(assemblyFolder, configFileName);
            if (!System.IO.File.Exists(sourceTxtFile))
            {
                Debug.WriteLine("File not found: " + sourceTxtFile);
                throw new Exception("Не найден файл " + sourceTxtFile);
            }
            return sourceTxtFile;
        }
    }
}
 
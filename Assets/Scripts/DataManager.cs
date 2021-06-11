using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/* This class is used to store data in text files */
public class DataManager
{
/// <summary>
    /// Saves all information regarding the trial in the participant's file in a dedicated row 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="navConfig"></param>
    /// <param name="data"></param>
    public static void WriteData(NavConfig navConfig, string fileName, string header, string data, bool root)
    {
        string participantName = navConfig.ParticipantName;
        string dataPath = "Assets/Data";
        CreateFolderIfNecessary(dataPath, participantName);
        string participantPath = dataPath + "/" + participantName;

        string path;
        if (root)
        {
            path = participantPath + "/" + fileName + ".csv";
        } else
        {
            string trial = navConfig.Path.Name + " (" + navConfig.Advice + ")";
            CreateFolderIfNecessary(participantPath, trial);
            path = participantPath + "/" + trial + "/" + fileName + ".csv";
        }
         
        bool fileExists = File.Exists(path);
        StreamWriter sw = new StreamWriter(path, true);

        if (!fileExists)
        {
            sw.Write(header + "\r\n");

        }

        sw.Write(data + "\r\n");
        sw.Flush();
        sw.Close();
    }
	
    private static void CreateFolderIfNecessary(string parentFolder, string newFolderName)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + newFolderName))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}

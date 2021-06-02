using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/* This class is used to store data in text files */
public class DataManager
{

    public static void WriteDataInFile(string fileName, NavConfig navConfig, string data)
    {
        string participantName = navConfig.ParticipantName;
        string dataPath = "Assets/Data";
        CreateFolderIfNecessary(dataPath, participantName);

        string trial = "Path" + navConfig.Path.Name + " (" + navConfig.Advice + ")";
        string participantPath = dataPath + "/" + participantName;
        CreateFolderIfNecessary(participantPath, trial);

        string path =  participantPath + "/" + trial + "/" + fileName + ".txt";
        if (!File.Exists(path))
        {
            File.WriteAllText(path, fileName + Environment.NewLine);
        }
        File.AppendAllText(path, data + Environment.NewLine);
    }

    private static void CreateFolderIfNecessary(string parentFolder, string newFolderName)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + newFolderName))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}

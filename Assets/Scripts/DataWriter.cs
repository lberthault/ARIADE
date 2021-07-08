using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/* This class is used to store data in text files */
public class DataWriter
{

    public static void WriteData(string fileName, string header, string data, bool root)
    {
        TrialConfig conf = GameManager.Instance.TrialConfig;
        string participantName = conf.ParticipantName;
        string dataPath = "Assets/Data";
        CreateFolderIfNecessary(dataPath, participantName);
        string participantPath = dataPath + "/" + participantName;

        string path;
        if (root)
        {
            path = participantPath + "/" + fileName + ".csv";
        } else
        {
            string trial = conf.Advice + " (" + conf.Path + ")";
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

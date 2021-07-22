using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/* This class is used to store data in text files */
public class DataWriter
{

    public static void WriteDataSingleLine(string fileName, string header, string data, bool root)
    {
        TrialConfig conf = GameManager.Instance.TrialConfig;
        string participantName = conf.ParticipantName;
        string dataPath = "Assets/Data";
        CreateFolderIfNecessary(dataPath, participantName);
        string participantPath = dataPath + "/" + participantName;

        string path;
        if (root)
        {
            path = participantPath + "/" + participantName + "_" + fileName + ".csv";
        } else
        {
            string trial = participantName + "_" + conf.Advice + "_" + conf.PathName;
            if (conf.PathName == Path.PathName.M)
            {
                trial = participantName + "_" + "BASELINE";
            }
            CreateFolderIfNecessary(participantPath, trial);
            path = participantPath + "/" + trial + "/" + trial + "_" + fileName + ".csv";
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

    public static void WriteDataMultipleLines(string fileName, string title, string header, string[] data, bool root)
    {
        TrialConfig conf = GameManager.Instance.TrialConfig;
        string participantName = conf.ParticipantName;
        string dataPath = "Assets/Data";
        CreateFolderIfNecessary(dataPath, participantName);
        string participantPath = dataPath + "/" + participantName;

        string path;
        if (root)
        {
            path = participantPath + "/" + participantName + "_" + fileName + ".csv";
        }
        else
        {
            string trial = participantName + "_" + conf.Advice + "_" + conf.PathName;
            if (conf.PathName == Path.PathName.M)
            {
                trial = participantName + "_" + "BASELINE";
            }
            CreateFolderIfNecessary(participantPath, trial);
            path = participantPath + "/" + trial + "/" + trial + "_" + fileName + ".csv";
        }

        bool fileExists = File.Exists(path);
        StreamWriter sw = new StreamWriter(path, true);

        /*if (!fileExists)
        {
            sw.Write(header + "\r\n");

        }*/
        sw.Write(title + "\r\n");
        sw.Write(header + "\r\n");
        for (int i = 0; i < data.Length; i++)
        {
            sw.Write(data[i] + "\r\n");
        }
        sw.Write("\r\n\r\n");
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

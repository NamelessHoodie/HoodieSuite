﻿using HoodieSuite.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HoodieSuite.Properties;
using System.Xml.Linq;
using System.IO;
using System.Net;
using HoodieSuite.MVVM.View;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows;
using Octokit;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace HoodieSuite.MVVM.ViewModel
{
    public class DashboardViewModel
    {

        public Dictionary<string, Dictionary<string, ToolEntry>> GameToolPage { get; }

        public static Dictionary<string, Dictionary<string, ToolEntry>> ToolDefParserAndDownloader()
        {


            //Get Tools Folder Path
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string absoluteToolsFolderPath = Path.Combine(exeDirectory, "Tools");

            //Initialize Serializer 
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            //Create list to deserialize the json into
            List<JsonToolsModel> listJson;

            //Deserialize json
            using (TextReader reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Definitions", "ToolsDef.json")))
            using (var jsonRreader = new Newtonsoft.Json.JsonTextReader(reader))
            {
                listJson = serializer.Deserialize<List<JsonToolsModel>>(jsonRreader);
            }

            //Populate Dictionary of Dictionaries of ToolEntry from Deserialized Json
            Dictionary<string, Dictionary<string, ToolEntry>> gameList = new Dictionary<string, Dictionary<string, ToolEntry>>();

            Dictionary<string, ToolEntry> allTools = new Dictionary<string, ToolEntry>();

            //Tool Lists
            foreach (var toolsModel in listJson)
            {
                //Supported Games in the curent Tool List
                foreach (var game in toolsModel.SupportedGames)
                {

                    //If the game does not have a Dictionary of ToolEntries yet, initialize it
                    if (!gameList.ContainsKey(game))
                        gameList.Add(game, new Dictionary<string, ToolEntry>());

                    var gameEntryToolsList = gameList[game];
                    foreach (var (toolName, tool) in toolsModel.Tools)
                    {
                        ToolEntry toolEntry;

                        //Check if the all tools Dictionary already contains an instance of that tool and assigns it to toolEntry
                        if (allTools.ContainsKey(tool.ToolArchiveUrl))
                            toolEntry = allTools[tool.ToolArchiveUrl];
                        else
                        {
                            toolEntry = new ToolEntry()
                            {
                                ToolName = toolName,
                                ToolDescription = tool.ToolDescription,
                                ToolDownloadUrl = tool.ToolArchiveUrl,
                                ToolVersion = tool.ToolVersion,
                                ToolPath = tool.ToolPath,
                            };
                            allTools.Add(tool.ToolArchiveUrl, toolEntry);
                        }

                        if (!gameEntryToolsList.ContainsKey(toolName))
                            gameEntryToolsList.Add(toolName, toolEntry);
                    }
                }
            }

            //foreach (var (gameName, toolEntries) in gameList)
            //{
            //    foreach (var (name, tool) in toolEntries)
            //    {
            //        TryDownloadTool(absoluteToolsFolderPath, tool);
            //    }
            //}

            return gameList;
        }

        static string GetFileNameFromUrl(string url)
        {
            Uri SomeBaseUri = new Uri("http://canbeanything");
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(SomeBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }

        private static bool AskUserIfDownload(ToolEntry tool, string oldVersionStr = null)
        {
            string text;
            string label;

            if (oldVersionStr != null)
            {
                text = $"New version of {tool.ToolName} found {tool.ToolVersion}\nWould you like to download it? The current Version is {oldVersionStr}.";
                label = $"Update found!";
            }
            else
            {
                text = $"{tool.ToolName} is not installed\nWould you like to download it?";
                label = $"{tool.ToolName} not found.";
            }
            if (MessageBox.Show(text, label, MessageBoxButtons.YesNo) == DialogResult.Yes)
                return true;
            return false;
        }

        public static bool TryDownloadTool(string toolsFolderPath, ToolEntry tool, bool isUserPromptDl = true)
        {
            string absoluteToolPath = Path.Combine(toolsFolderPath, tool.ToolPath.Split('\\').First());
            string absoluteVersionFilePath = Path.Combine(absoluteToolPath, $"{tool.ToolName.Replace(' ', '_')}.hsvf");
            if (!tool.isDownloaded)
            {
                if (isUserPromptDl ? AskUserIfDownload(tool) : true)
                {
                    WriteToolToDisk(tool, absoluteToolPath, absoluteVersionFilePath);
                }
            }
            return false;
            //return TryUpdateTool(toolsFolderPath, tool, isUserPromptDl);
        }

        private static void WriteToolToDisk(ToolEntry tool, string absoluteToolPath, string absoluteVersionFilePath)
        {
            Directory.CreateDirectory(absoluteToolPath);
            if (!File.Exists(absoluteVersionFilePath))
                File.Create(absoluteVersionFilePath).Dispose();
            File.WriteAllText(absoluteVersionFilePath, tool.ToolVersion);
            string downloadFileName = GetFileNameFromUrl(tool.ToolDownloadUrl);
            string downloadFilePath = Path.Combine(absoluteToolPath, downloadFileName);
            HoodieShared.Core.DownloadWithProgressBar(tool.ToolName, tool.ToolDownloadUrl, downloadFilePath);
            HoodieShared.Core.ExtractFile(AppDomain.CurrentDomain.BaseDirectory, downloadFilePath, absoluteToolPath);
            File.Delete(downloadFilePath);
            tool.NotifyDownloadedStateChanged();
        }

        public static bool TryUpdateTool(string toolsFolderPath, ToolEntry tool, bool isUserPromptDl = true)
        {
            string absoluteToolPath = Path.Combine(toolsFolderPath, tool.ToolPath.Split('\\').First());
            string absoluteToolPathExe = Path.Combine(toolsFolderPath, tool.ToolPath);
            string absoluteVersionFilePath = Path.Combine(absoluteToolPath, $"{tool.ToolName.Replace(' ', '_')}.hsvf");
            if (Directory.Exists(absoluteToolPath))
            {
                string versionFileText = File.ReadAllText(absoluteVersionFilePath).Trim();
                if (versionFileText != tool.ToolVersion)
                {
                    ComeBackLol:
                    try
                    {
                        File.Delete(absoluteToolPathExe);
                    }
                    catch (Exception)
                    {
                        if (MessageBox.Show($"WARNING: Could not update {tool.ToolName}.\nVersion {versionFileText} => {tool.ToolVersion}\n{tool.ToolPath} was already being accessed.", $"WARNING: Could not update { tool.ToolName}", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                        {
                            goto ComeBackLol;
                        }
                        return false;
                    }
                    if (isUserPromptDl ? AskUserIfDownload(tool, versionFileText) : true)
                    {
                        Directory.Delete(absoluteToolPath, true);
                        WriteToolToDisk(tool, absoluteToolPath, absoluteVersionFilePath);
                    }
                    return true;
                }
            }
            return false;
        }

        public List<string> SupportedGames { get; set; }
        public string SelectedGame { get; set; }
        public ToolEntry SelectedTool { get; set; }
        public void HoodieUpdater_Update()
        {
            bool isUpdate = true;
            string updaterFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HoodieUpdater");
            if (!Directory.Exists(updaterFolderPath))
            {
                Directory.CreateDirectory(updaterFolderPath);
            }
            else if (isUpdate)
            {
                Directory.Delete(updaterFolderPath, true);
                Directory.CreateDirectory(updaterFolderPath);
            }
        }
        public DashboardViewModel()
        {
            GameToolPage = ToolDefParserAndDownloader();
            SupportedGames = GameToolPage.Keys.ToList();
        }
    }
}

﻿using System;
using System.Collections.Generic;

namespace RSDKv1
{
    public class GameConfig
    {
        public class Category
        {
            public class SceneInfo
            {
                /// <summary>
                /// not entirely sure
                /// </summary>
                public byte Unknown;
                /// <summary>
                /// the folder of the scene
                /// </summary>
                public string SceneFolder = "Folder";
                /// <summary>
                /// the scene's identifier (E.G Act1 or Act2)
                /// </summary>
                public string ActID = "1";
                /// <summary>
                /// the scene name (shows up on the dev menu)
                /// </summary>
                public string Name = "Scene";

                public SceneInfo()
                {
                    SceneFolder = "";
                    ActID = "";
                    Name = "";
                    Unknown = 0;
                }

                public SceneInfo(Reader reader)
                {
                    SceneFolder = reader.ReadRSDKString();
                    ActID = reader.ReadRSDKString();
                    Name = reader.ReadRSDKString();
                    Unknown = reader.ReadByte();
                    //Console.WriteLine("Name = " + Name + " ,Act ID = " + ActID + " ,Scene Folder = " + SceneFolder);
                }

                public void Write(Writer writer)
                {
                    writer.WriteRSDKString(SceneFolder);
                    writer.WriteRSDKString(ActID);
                    writer.WriteRSDKString(Name);
                    writer.Write(Unknown);
                }
            }

            /// <summary>
            /// the list of stages in this category
            /// </summary>
            public List<SceneInfo> Scenes = new List<SceneInfo>();

            public Category()
            {
                Scenes = new List<SceneInfo>();
            }

            public Category(string filename) : this(new Reader(filename))
            {

            }

            public Category(System.IO.Stream stream) : this(new Reader(stream))
            {

            }

            internal Category(Reader reader)
            {
                byte SceneCount = reader.ReadByte();

                for (int i = 0; i < SceneCount; i++)
                {
                    Scenes.Add(new SceneInfo(reader));
                }

            }

            public void Write(string filename)
            {
                using (Writer writer = new Writer(filename))
                    this.Write(writer);
            }

            public void Write(System.IO.Stream stream)
            {
                using (Writer writer = new Writer(stream))
                    this.Write(writer);
            }

            internal void Write(Writer writer)
            {
                writer.Write((byte)Scenes.Count);
                for (int i = 0; i < Scenes.Count; i++)
                {
                    Scenes[i].Write(writer);
                }
            }

        }

        public class GlobalVariable
        {
            /// <summary>
            /// the name of the variable
            /// </summary>
            public string Name;
            /// <summary>
            /// the variable's value
            /// </summary>
            public int Value = 0;

            public GlobalVariable()
            {

            }

            public GlobalVariable(string name)
            {
                Name = name;
            }

            public GlobalVariable(Reader reader)
            {
                Name = reader.ReadString();
                //Console.WriteLine(Name);
                Value = reader.ReadInt32();
            }

            public void Write(Writer writer)
            {
                writer.WriteRSDKString(Name);
                writer.Write(Value);
            }
        }

        public class PlayerData
        {
            /// <summary>
            /// The location of the player sprite mappings
            /// </summary>
            public string PlayerAnimLocation;
            /// <summary>
            /// the location of the player script
            /// </summary>
            public string PlayerScriptLocation;
            /// <summary>
            /// the name of the player
            /// </summary>
            public string PlayerName;

            public PlayerData()
            {
                PlayerAnimLocation = "";
                PlayerScriptLocation = "";
                PlayerName = "";
            }

            public PlayerData(Reader reader)
            {
                PlayerAnimLocation = reader.ReadRSDKString();
                PlayerScriptLocation = reader.ReadRSDKString();
                PlayerName = reader.ReadRSDKString();
            }

            public void Write(Writer writer)
            {
                writer.WriteRSDKString(PlayerAnimLocation);
                writer.WriteRSDKString(PlayerScriptLocation);
                writer.WriteRSDKString(PlayerName);
            }

        }

        /// <summary>
        /// the game name, appears on the window
        /// </summary>
        public string GameWindowText;
        /// <summary>
        /// i have no idea
        /// </summary>
        public string DataFileName;
        /// <summary>
        /// the string the appears in the about window
        /// </summary>
        public string GameDescriptionText;

        /// <summary>
        /// the list of filepaths for the global objects
        /// </summary>
        public List<string> ScriptPaths = new List<string>();
        /// <summary>
        /// the list of global SoundFX
        /// </summary>
        public List<string> SoundFX = new List<string>();
        /// <summary>
        /// the list of global variable names and values
        /// </summary>
        public List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();
        /// <summary>
        /// the list of playerdata needed for players
        /// </summary>
        public List<PlayerData> playerData = new List<PlayerData>();
        /// <summary>
        /// the category list (stage list)
        /// </summary>
        public List<Category> Categories = new List<Category>();

        public GameConfig()
        {
            Categories.Add(new Category()); //Menus
            Categories.Add(new Category()); //Stages              
            Categories.Add(new Category()); //Special Stages
            Categories.Add(new Category()); //Bonus Stages
        }

        public GameConfig(string filename) : this(new Reader(filename))
        {
        }

        public GameConfig(System.IO.Stream stream) : this(new Reader(stream))
        {

        }

        public GameConfig(Reader reader)
        {
            GameWindowText = reader.ReadRSDKString();
            DataFileName = reader.ReadRSDKString();
            GameDescriptionText = reader.ReadRSDKString();

            //Console.WriteLine("Game Title: " + GameWindowText);
            //Console.WriteLine("DataFile Name: " + DataFileName);
            //Console.WriteLine("Game Description: " + GameDescriptionText);

            this.ReadObjectsNames(reader);

            byte Globals_Amount = reader.ReadByte();

            for (int i = 0; i < Globals_Amount; i++)
            {
                GlobalVariables.Add(new GlobalVariable(reader));
            }

            this.ReadWAVConfiguration(reader);

            byte PLR_data_count = reader.ReadByte();

            for (int i = 0; i < PLR_data_count; ++i)
            {
                playerData.Add(new PlayerData(reader));
            }

            Categories.Add(new Category(reader)); //Menus
            Categories.Add(new Category(reader)); //Stages              
            Categories.Add(new Category(reader)); //Special Stages
            Categories.Add(new Category(reader)); //Bonus Stages

            reader.Close();

        }

        public void Write(string filename)
        {
            using (Writer writer = new Writer(filename))
                this.Write(writer);
        }

        public void Write(System.IO.Stream stream)
        {
            using (Writer writer = new Writer(stream))
                this.Write(writer);
        }

        public void Write(Writer writer)
        {
            writer.WriteRSDKString(GameWindowText);
            writer.WriteRSDKString(DataFileName);
            writer.WriteRSDKString(GameDescriptionText);

            this.WriteObjectsNames(writer);

            writer.Write((byte)GlobalVariables.Count);

            for (int i = 0; i < GlobalVariables.Count; i++)
            {
                GlobalVariables[i].Write(writer);
            }

            this.WriteWAVConfiguration(writer);

            writer.Write((byte)playerData.Count);

            for (int i = 0; i < playerData.Count; ++i)
            {
                playerData[i].Write(writer);
            }

            for (int i = 0; i < 4; i++)
            {
                Categories[i].Write(writer);
            }

            writer.Close();

        }

        internal void ReadObjectsNames(Reader reader)
        {
            byte srctxt_count = reader.ReadByte();
            for (int i = 0; i < srctxt_count; ++i)
            { ScriptPaths.Add(reader.ReadRSDKString()); /*Console.WriteLine(ScriptPaths[i]);*/ }
        }

        internal void WriteObjectsNames(Writer writer)
        {
            writer.Write((byte)ScriptPaths.Count);
            foreach (string name in ScriptPaths)
                writer.WriteRSDKString(name);
        }

        internal void ReadWAVConfiguration(Reader reader)
        {
            byte wavs_count = reader.ReadByte();

            for (int i = 0; i < wavs_count; ++i)
            { SoundFX.Add(reader.ReadString()); }
        }

        internal void WriteWAVConfiguration(Writer writer)
        {
            writer.Write((byte)SoundFX.Count);
            foreach (string wav in SoundFX)
                writer.Write(wav);
        }

    }
}

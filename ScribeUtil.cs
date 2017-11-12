﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Verse;

namespace ServerMod
{
    public static class ScribeUtil
    {
        private static MemoryStream stream;
        private static readonly FieldInfo writerField = typeof(ScribeSaver).GetField("writer", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void StartWriting()
        {
            stream = new MemoryStream();

            Scribe.mode = LoadSaveMode.Saving;
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "  ";
            xmlWriterSettings.OmitXmlDeclaration = true;
            XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings);
            writerField.SetValue(Scribe.saver, writer);
            writer.WriteStartDocument();
        }

        public static byte[] FinishWriting()
        {
            Scribe.saver.FinalizeSaving();
            byte[] arr = stream.ToArray();
            stream = null;
            return arr;
        }

        public static void StartLoading(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (XmlTextReader xml = new XmlTextReader(stream))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(xml);
                Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
            }

            Scribe.mode = LoadSaveMode.LoadingVars;
        }

        public static void FinishLoading()
        {
            Scribe.loader.FinalizeLoading();
        }

        public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode, LookMode valueLookMode)
        {
            List<K> list1 = null;
            List<V> list2 = null;
            Look(ref dict, label, keyLookMode, valueLookMode, ref list1, ref list2);
        }

        public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode, LookMode valueLookMode, ref List<K> keysWorkingList, ref List<V> valuesWorkingList)
        {
            if (Scribe.EnterNode(label))
            {
                try
                {
                    if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        keysWorkingList = new List<K>();
                        valuesWorkingList = new List<V>();
                    }
                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        foreach (KeyValuePair<K, V> current in dict)
                        {
                            keysWorkingList.Add(current.Key);
                            valuesWorkingList.Add(current.Value);
                        }
                    }
                    Scribe_Collections.Look<K>(ref keysWorkingList, "keys", keyLookMode, new object[0]);
                    Scribe_Collections.Look<V>(ref valuesWorkingList, "values", valueLookMode, new object[0]);
                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        if (keysWorkingList != null)
                        {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }
                        if (valuesWorkingList != null)
                        {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }
                    bool flag = keyLookMode == LookMode.Reference || valueLookMode == LookMode.Reference;
                    if ((flag && Scribe.mode == LoadSaveMode.ResolvingCrossRefs) || (!flag && Scribe.mode == LoadSaveMode.LoadingVars))
                    {
                        dict.Clear();
                        if (keysWorkingList == null)
                        {
                            Log.Error("Cannot fill dictionary because there are no keys.");
                        }
                        else if (valuesWorkingList == null)
                        {
                            Log.Error("Cannot fill dictionary because there are no values.");
                        }
                        else
                        {
                            if (keysWorkingList.Count != valuesWorkingList.Count)
                            {
                                Log.Error(string.Concat(new object[]
                                {
                                    "Keys count does not match the values count while loading a dictionary (maybe keys and values were resolved during different passes?). Some elements will be skipped. keys=",
                                    keysWorkingList.Count,
                                    ", values=",
                                    valuesWorkingList.Count
                                }));
                            }
                            int num = Math.Min(keysWorkingList.Count, valuesWorkingList.Count);
                            for (int i = 0; i < num; i++)
                            {
                                if (keysWorkingList[i] == null)
                                {
                                    Log.Error(string.Concat(new object[]
                                    {
                                        "Null key while loading dictionary of ",
                                        typeof(K),
                                        " and ",
                                        typeof(V),
                                        "."
                                    }));
                                }
                                else
                                {
                                    try
                                    {
                                        dict.Add(keysWorkingList[i], valuesWorkingList[i]);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(string.Concat(new object[]
                                        {
                                            "Exception in LookDictionary(node=",
                                            label,
                                            "): ",
                                            ex
                                        }));
                                    }
                                }
                            }
                        }
                    }
                    if (Scribe.mode == LoadSaveMode.PostLoadInit)
                    {
                        if (keysWorkingList != null)
                        {
                            keysWorkingList.Clear();
                            keysWorkingList = null;
                        }
                        if (valuesWorkingList != null)
                        {
                            valuesWorkingList.Clear();
                            valuesWorkingList = null;
                        }
                    }
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                dict = null;
            }
        }
    }
}
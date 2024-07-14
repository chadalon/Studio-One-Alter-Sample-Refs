using System.IO.Compression;
using System.Text;
using System.Xml;

// todo check for wonky symbols in file names

//date modified??

//MISSING IN PLUG-INS: SAMPLE1 AND IMPACT. I THINK THIS IS POSSIBLE DOOD
namespace Studio_One_Alter_Sample_Refs
{
	internal class Program
	{
		static uint _refUpdateCount = 0;
		const int XML_WRAP_CHARACTER_COUNT = 100;
		const string MEDIA_POOL = "Song/mediapool.xml";
		static List<string> _newSampleFolders = new();
		static Dictionary<string, string?> _discoveredFiles = new();
		static int count = 0;
		static void Main(string[] args)
		{
			Console.WriteLine("WARNING!!! THIS PROGRAM ATTEMPTS TO OVERWRITE STUDIO ONE .SONG FILES. BY USING THIS PROGRAM YOU ASSUME ALL RISK AND ARE WILLING TO CORRUPT OR DELETE YOUR SONG FILES");
			Console.WriteLine("I AM NOT LIABLE FOR ANY DAMAGES CAUSED BY THIS PROGRAM. IF YOU DON'T KNOW WHAT YOU ARE DOING YOU SHOULD NOT PROCEED. BACKUP YOUR SONGS BEFORE PROCEEDING!!!!!");
			Console.WriteLine("\nThis program is in no way affiliated with Presonus or Studio One. It may or may not work, I'd recommend trying this on a few projects as a test before going full send.");
			Console.WriteLine("Now let's get to work getting rid of that super annoying process of manually finding all your samples for EVERY song ;)\n");
			_newSampleFolders.Add("C:\\Users\\chadm\\Desktop\\Samples");

			for (int i = 0; i < _newSampleFolders.Count; i++)
			{
				_newSampleFolders[i] = Path.GetFullPath(_newSampleFolders[i]);
			}
			string folderPath;
			while (true)
			{
				Console.WriteLine("Enter a folder path for ur (outdated) studio one songs:");
				folderPath = Console.ReadLine();
				if (folderPath == null || !Directory.Exists(folderPath))
				{
					Console.WriteLine("Invalid option.");
					continue;
				}
				else
				{
					break;
				}
			}
			// TODO re-prompt user with the current settings and ask them to proceed (with another warning probs)
			foreach (string songFolderPath in Directory.GetDirectories(folderPath))
			{
				var songFile = Directory.GetFiles(songFolderPath, "*.song").FirstOrDefault();
				//throw new Exception("need to exclude ._*.song files...");
				if (songFile == null)
				{
					// TODO maybe check autosaves here
					Console.WriteLine($"No song file found in {songFolderPath} (it may have some autosaves)...skipping to next");
					continue;
				}

				LoadProject(songFile);
				/*
				try
				{
				}
				catch (Exception ex)
				{
					Console.WriteLine("\nAn exception occured:");
					Console.WriteLine(ex.Message);
					break;
				}*/
				count++;
				if (count > 2)
					break;

			}
			Console.WriteLine($"\nUpdated {_refUpdateCount} sample references.");
			Console.WriteLine("\nPress enter to exit...");
			Console.ReadLine();
		}
		public static void LoadProject(string sourceFilePath)
		{
			Console.WriteLine("\n*****************************************");
			Console.WriteLine($"Finding samples for {sourceFilePath}");
			Console.WriteLine("\n*****************************************\n");
			// TODO test inputting both / and \\
			string tempFilePath = sourceFilePath + "temp"; // will this work lmao
			using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open))
			using (FileStream destinationStream = new FileStream(tempFilePath, FileMode.Create))
			using (ZipArchive source = new ZipArchive(sourceStream, ZipArchiveMode.Read))
			using (ZipArchive destination = new ZipArchive(destinationStream, ZipArchiveMode.Create))
			{
				foreach (ZipArchiveEntry entry in source.Entries)
				{
					if (entry.FullName == MEDIA_POOL)
					{
						// modify
						var destinationEntry = destination.CreateEntry(entry.FullName);

						using (var writer = new StreamWriter(destinationEntry.Open(), Encoding.UTF8))
						using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
						{
							writer.Write(AlterFile(reader));
						}
					}
					else
					{
						var newEntry = destination.CreateEntry(entry.FullName); // TODO change compressionlevel this maybe
						using (var entryStream = entry.Open())
						using (var newEntryStream = newEntry.Open())
						{
							entryStream.CopyTo(newEntryStream);
						}
					}
				}
			}
			File.Delete(sourceFilePath);
			File.Move(tempFilePath, sourceFilePath);
		}
		static string AlterFile(StreamReader mediaPoolData)
		{
			string? SearchMyDirOfficer(DirectoryInfo currentDir, string fileName)
			{
				var triedFile = currentDir.EnumerateFiles().ToList().FirstOrDefault(x => x.Name == fileName);
				if (triedFile != null)
				{
					return triedFile.FullName;
				}
				var dirs = currentDir.EnumerateDirectories();
				foreach (var dir in dirs)
				{
					var res = SearchMyDirOfficer(dir, fileName);
					if (res != null) return res;
				}
				return null;
			}
			XmlReaderSettings settings = new XmlReaderSettings { NameTable = new NameTable() };
			XmlNamespaceManager xmlns = new XmlNamespaceManager(settings.NameTable);
			xmlns.AddNamespace("x", "peepeepoopoo");
			XmlParserContext context = new XmlParserContext(null, xmlns, "", XmlSpace.Default);
			XmlReader reader = XmlReader.Create(mediaPoolData, settings, context);
			XmlDocument xmlDoc = new();
			xmlDoc.Load(reader);
			XmlNodeList elements = xmlDoc.SelectNodes("//AudioClip/Url");
			foreach (XmlNode element in elements)
			{
				string fpath = element.Attributes?.GetNamedItem("url")?.Value;
				if (fpath == null) continue;
				string[] dirName = fpath.Split('/');
				string fileName = dirName[dirName.Length - 1];
				string? matchingFile;
				if (_discoveredFiles.TryGetValue(fileName, out matchingFile))
				{
					Console.WriteLine($"{fileName} was cached, nice.");
				}
				else
				{
					foreach (var path in _newSampleFolders)
					{
						matchingFile = SearchMyDirOfficer(new DirectoryInfo(path), fileName);
						if (matchingFile != null) break;
					}
					_discoveredFiles[fileName] = matchingFile; // cache to make our other file searches a billion times faster
				}
				if (matchingFile != null)
				{
					Console.WriteLine($"FOUND A MATCH!!! {fileName} found in {matchingFile}");
					// rewrite
					element.Attributes.GetNamedItem("url").Value = "file:///" + matchingFile.Replace("\\", "/");
					_refUpdateCount++;
				}
				else
				{
					Console.WriteLine($"Couldn't find a match for {fileName} ...");
				}
			}
			return Beautify(xmlDoc);
		}
		/// <summary>
		/// I hate this. I want to copy s1's format EXACTLY (or as close as possible)
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		static public string Beautify(XmlDocument doc)
		{
			string GetInnerXML(XmlNode node, int tabCount)
			{
				StringBuilder myNodeString = new();
				myNodeString.Append('\t', tabCount);
				myNodeString.Append($"<{node.Name}");
				// attribs (wrapping when necessary
				int charCount = 0;
				foreach (XmlAttribute attrib in node.Attributes)
				{
					string toAppend = "";
					if (charCount + node.Name.Length >= XML_WRAP_CHARACTER_COUNT)
					{
						charCount = 0;
						myNodeString.Append("\r\n");
						myNodeString.Append('\t', tabCount);
						myNodeString.Append(' ', 12); //idk alright? this is just how studio one does (or did) it
						if (node.Name == "AudioPartClip")
						{
							myNodeString.Append(' ', 3); // this is not maintainable ;)
						}
					}
					else
					{
						toAppend = " ";
					}
					toAppend += $"{attrib.Name}=\"{attrib.Value.Replace("&", "&amp;")}\"";
					charCount += toAppend.Length;
					myNodeString.Append(toAppend);
				}
				if (node.ChildNodes.Count == 0)
				{
					myNodeString.Append("/>\r\n");
				}
				else
				{
					myNodeString.Append(">\r\n");
					foreach (XmlNode xmlNode in node.ChildNodes)
					{
						myNodeString.Append(GetInnerXML(xmlNode, tabCount + 1));
					}
					myNodeString.Append('\t', tabCount);
					myNodeString.Append($"</{node.Name}>\r\n");
				}
				return myNodeString.ToString();
			}
			int tabCount = 0;
			return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + GetInnerXML(doc.DocumentElement, tabCount);
		}
	}
}
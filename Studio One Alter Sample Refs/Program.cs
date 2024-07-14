using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Studio_One_Alter_Sample_Refs
{
	internal class Program
	{
		static int counter = 0;
		const int XML_WRAP_CHARACTER_COUNT = 100;
		const string MEDIA_POOL = "Song/mediapool.xml";
		static void Main(string[] args)
		{
			string folderPath;
			while (true)
			{
				Console.WriteLine("Enter a filepath for ur studio one songs:");
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
			foreach (string songFolderPath in Directory.GetDirectories(folderPath))
			{
				var songFile = Directory.GetFiles(songFolderPath, "*.song").FirstOrDefault();
				if (songFile == null)
				{
					// TODO maybe check autosaves here
					Console.WriteLine($"No song file found in {songFolderPath}");
					continue;
				}

				songFile = "D:\\Everything\\Studio Songs\\Chadsture house\\Chadsture house.song";
				string outputFile = "C:\\Users\\chadm\\Desktop\\finalNewChadsture.zip";
				LoadProject(songFile, outputFile);
				break;
				/*
				return;
				byte[] fileContent = File.ReadAllBytes(songFile);
				string xmlContent = ExtractXmlContent(fileContent);
				//Console.WriteLine(xmlContent);

				string originalContent = File.ReadAllText(songFile);
				foreach (var character in originalContent)
				{
					Console.Write(character);
				}
				//Console.WriteLine(originalContent);
				string xmlContent = ExtractXmlContent(originalContent);
				Console.WriteLine($"xml content: {xmlContent}");*/

			}
		}
		public static void LoadProject(string sourceFilePath, string destFilePath)
		{

			using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open))
			using (FileStream destinationStream = new FileStream(destFilePath, FileMode.Create))
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

			/*
				byte[] archiveFile;
			var archiveStream = new MemoryStream();
			//var destination = new ZipA
			Console.WriteLine($"{filePath}");
			using (ZipArchive zipArchive = ZipFile.OpenRead(filePath))
			{


				//zipArchive.Entries.ToList().ForEach(x => Console.WriteLine(x));
				string totalString = "";
				using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
				{
					foreach (var projectEntry in zipArchive.Entries)
					{
						if (projectEntry.FullName == MEDIA_POOL)
						{
							// edit archive
							continue;
						}
						else
						{
							var newEntry = 
								projectEntry;
							using var zipStream = archiveEntry.Open();
							zipStream.w
						}
						//totalString += $"\n{projectEntry}:\n";
						Console.WriteLine($"\n{projectEntry}");
						continue;
						Stream entryStream = projectEntry.Open();
						using (StreamReader reader = new StreamReader(entryStream, Encoding.UTF8, true))
						{
							totalString += reader.ReadToEnd();
								//s.Write(reader.ReadToEnd());
						}
						
						using (StreamReader reader = StripBom(entryStream))
						{
							Console.WriteLine(reader.ReadLine());
							//XmlSerializer serializer = new XmlSerializer(typeof(Project));
							//return (Project)serializer.Deserialize(reader);
						}

					}
				}
	/*
				string xmlDir = "C:\\Users\\chadm\\Desktop\\s1output.txt";
				using (StreamWriter s = new StreamWriter(xmlDir))
				{
					s.WriteLine(totalString);
				}
				/*
				using (var fileStream = new FileStream("C:\\Users\\chadm\\Desktop\\s1songoutput.txt", FileMode.Create))
				{
					fileStream.WriteLine(zipArchive.ToString());
				}*/

				/*
				//ZipArchiveEntry projectEntry = zipArchive.GetEntry("PROJECT_FILE"); // Adjust the name as needed
				if (projectEntry == null)
				{
					throw new FileNotFoundException("Project file not found in the archive.");
				}
				using (Stream entryStream = projectEntry.Open())
				using (StreamReader reader = StripBom(entryStream))
				{
					//XmlSerializer serializer = new XmlSerializer(typeof(Project));
					//return (Project)serializer.Deserialize(reader);
				}
			}
			archiveStream.Dispose();
				*/
		}
		public static StreamReader StripBom(Stream inputStream)
		{
			using (BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true))
			{
				byte[] buffer = new byte[4];
				int bytesRead = reader.Read(buffer, 0, 4);

				Encoding encoding = Encoding.UTF8;
				int bomLength = 0;

				if (bytesRead >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf) // UTF-8 BOM
				{
					encoding = Encoding.UTF8;
					bomLength = 3;
				}
				else if (bytesRead >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe) // UTF-16 LE BOM
				{
					encoding = Encoding.Unicode;
					bomLength = 2;
				}
				else if (bytesRead >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff) // UTF-16 BE BOM
				{
					encoding = Encoding.BigEndianUnicode;
					bomLength = 2;
				}
				else if (bytesRead == 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xfe && buffer[3] == 0xff) // UTF-32 BE BOM
				{
					encoding = new UTF32Encoding(true, true);
					bomLength = 4;
				}
				else if (bytesRead == 4 && buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0x00 && buffer[3] == 0x00) // UTF-32 LE BOM
				{
					encoding = new UTF32Encoding(false, true);
					bomLength = 4;
				}

				// Create a MemoryStream and write the remaining bytes from the input stream
				MemoryStream memoryStream = new MemoryStream();
				memoryStream.Write(buffer, bomLength, bytesRead - bomLength);

				byte[] tempBuffer = new byte[4096]; // Buffer size for reading chunks from input stream
				int bytesReadInChunk;

				while ((bytesReadInChunk = reader.Read(tempBuffer, 0, tempBuffer.Length)) > 0)
				{
					memoryStream.Write(tempBuffer, 0, bytesReadInChunk);
				}

				memoryStream.Position = 0; // Reset the memory stream position

				return new StreamReader(memoryStream, encoding);
			}
		}
		static string AlterFile(StreamReader mediaPoolData)
		{
			XmlReaderSettings settings = new XmlReaderSettings { NameTable = new NameTable() };
			XmlNamespaceManager xmlns = new XmlNamespaceManager(settings.NameTable);
			xmlns.AddNamespace("x", "peepeepoopoo");
			XmlParserContext context = new XmlParserContext(null, xmlns, "", XmlSpace.Default);
			XmlReader reader = XmlReader.Create(mediaPoolData, settings, context);
			//XElement xmlTree = XElement.Parse(reader);
			//xmlTree.DescendantNodes()
			XmlDocument xmlDoc = new();
			//Console.WriteLine(mediaPoolData.ReadToEnd());
			xmlDoc.Load(reader);
			XmlNodeList elements = xmlDoc.SelectNodes("//*");
			foreach (XmlNode element in elements)
			{
				//Console.WriteLine(element.Name);
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
							myNodeString.Append(' ', 3); // this is not maintainable
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
			//doc.namesp
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\r\n",
				OmitXmlDeclaration = true
			};
			using (XmlWriter writer = XmlWriter.Create(sb, settings))
			{
				doc.Save(writer);
			}
			string stringTheory = sb.ToString().Replace(" xmlns:x=\"peepeepoopoo\"","").Replace("\" />", "\"/>"); // probs gonna take a year
			return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + stringTheory + "\r\n";
		}
		static string ExtractXmlContent(byte[] content)
		{
			string contentStr = Encoding.UTF8.GetString(content);
			foreach (var c in contentStr)
			{
				Console.Write(c);
				counter++;
				if (counter > 200) break;
			}
			Match match = Regex.Match(contentStr, "<\\?xml.*?</Song>", RegexOptions.Singleline);
			return match.Success ? match.Value : null;
		}
	}
}
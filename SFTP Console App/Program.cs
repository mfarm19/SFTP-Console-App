using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet.Sftp;
using System.IO;
using System.Text;

class Program
{
	private static List<SftpFile> retryList = new List<SftpFile>();
	private static StringBuilder log = new StringBuilder();

	// Declare SFTP servers; the user will be asked to input this in production
	private static SftpClient? client1 = null;
	private static SftpClient? client2 = null;

	// Declare DirectoryLocation for both servers; the user will be asked to input this in production
	// TODO: Consider the possibility that the user may want to use a directory that's two or more folders deep OR that they may want to use the root folder/default logon location
	private static string clientOneDirectoryLocation = System.Diagnostics.Debugger.IsAttached ? "FTPTests" : String.Empty;
	private static string clientTwoDirectoryLocation = System.Diagnostics.Debugger.IsAttached ? "FTPTests" : String.Empty;

	static void Main(string[] args)
	{
		// Connect to first SFTP server
		while (true)
		{
			try
			{
				client1 = connectToSFTPServer("SFTP Server 1");
				client1.Connect();
				Console.WriteLine("Connected to SFTP Server 1.");
				log.Append($"Connected to SFTP Server 1.{Environment.NewLine}");
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				log.Append($"Error: {ex.Message}");
			}
		}

		// Locate the download directory for the first server
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine("Type \"!help\" for a list of available directories on Server 1.");
		Console.ResetColor();
		while (clientOneDirectoryLocation == String.Empty || !client1.Exists(clientOneDirectoryLocation))
		{
			try
			{
				Console.Write($"What is the directory location for SFTP Server 1? ");
				clientOneDirectoryLocation = Console.ReadLine();
				if (clientOneDirectoryLocation == "!help")
				{
					var directories = client1.ListDirectory(".").Where(x => x.IsDirectory).Select(x => x.Name);
					foreach (var directory in directories)
					{
						Console.WriteLine(directory);
					}
				}
				else if (!client1.Exists(clientOneDirectoryLocation))
				{
					Console.Write($"Directory not found. ");
				}
				else
				{
					Console.WriteLine("SFTP Server 1 directory located.");
					log.Append($"SFTP Server 1 directory located..{Environment.NewLine}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				log.Append($"Error: {ex.Message}");
			}
		}

		// Connect to second SFTP server
		while (true)
		{
			try
			{
				client2 = connectToSFTPServer("SFTP Server 2");
				client2.Connect();
				Console.WriteLine("Connected to SFTP Server 2.");
				log.Append($"Connected to SFTP Server 2.{Environment.NewLine}");
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				log.Append($"Error: {ex.Message}");
			}
		}

		// Locate the upload directory for the second server
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine("Type \"!help\" for a list of available directories on Server 2.");
		Console.ResetColor();
		while (clientTwoDirectoryLocation == String.Empty || !client2.Exists(clientTwoDirectoryLocation))
		{
			try
			{
				Console.Write("What is the directory location for SFTP Server 2? ");
				clientTwoDirectoryLocation = Console.ReadLine();
				if (clientTwoDirectoryLocation == "!help")
				{
					var directories = client2.ListDirectory(".").Where(x => x.IsDirectory).Select(x => x.Name);
					foreach (var directory in directories)
					{
						Console.WriteLine(directory);
					}
				}
				else if (!client2.Exists(clientTwoDirectoryLocation))
				{
					Console.Write($"Directory not found. ");
				}
				else
				{
					Console.WriteLine("SFTP Server 2 directory located.");
					log.Append($"SFTP Server 2 directory located..{Environment.NewLine}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				log.Append($"Error: {ex.Message}");
			}
		}

		// Attempt to get all files from the given directory 
		IEnumerable<SftpFile> allFileList = client1.ListDirectory(clientOneDirectoryLocation);

		// Locate failedToProcessFiles.txt file if it exists
		string recoveredFileListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "failedToProcessFiles.txt");
		string[] recoveredFileList = File.Exists(recoveredFileListPath) ? File.ReadAllLines(recoveredFileListPath) : Array.Empty<string>();

		ConsoleKeyInfo reprocessResponse = new ConsoleKeyInfo();
		if (recoveredFileList.Count() > 0)
		{
			// Wait for next keystroked
			Console.Write($"There are {recoveredFileList.Count()} files that were not processed successfully. Try to process them again? (y/n) ");
			reprocessResponse = Console.ReadKey();

			while (reprocessResponse.KeyChar.ToString().ToLower() != "n" && reprocessResponse.KeyChar.ToString().ToLower() != "y")
			{
				Console.Write($"{Environment.NewLine}There are {recoveredFileList.Count()} files that were not processed successfully. Try to process them again? (y/n) ");
				reprocessResponse = Console.ReadKey();
			}

			if (reprocessResponse.KeyChar.ToString().ToLower() == "n")
			{
				Console.WriteLine($"{Environment.NewLine}Recoverd files were not processed.");
			}
			else
			{
				// Add a line break, it looks better this way
				Console.WriteLine("");

				// Attempt to locate the recovered files
				IEnumerable<SftpFile> fileList = allFileList.Where(file => recoveredFileList.Contains(file.Name)).ToList();

				// Attempt to process all recovered files
				processFiles(fileList);

				// Delete the failedToProcessFiles file
				File.Delete(recoveredFileListPath);
			}
		}

		if (recoveredFileList.Count() == 0 || reprocessResponse.KeyChar.ToString().ToLower() == "n")
		{
			// Look for files with a specific extension, we're using txt files as a placeholder
			// TODO: update with real file extension
			IEnumerable<SftpFile> txtFiles = allFileList.Where(file => file.Name.EndsWith(".txt"));

			// Wait for next keystroked
			Console.Write($"There are {txtFiles.Count()} files in the given directory. Process all? (y/n) ");
			ConsoleKeyInfo value = Console.ReadKey();

			while (value.KeyChar.ToString().ToLower() != "n" && value.KeyChar.ToString().ToLower() != "y")
			{
				Console.Write($"{Environment.NewLine}There are {txtFiles.Count()} files in the given directory. Process all? (y/n) ");
				value = Console.ReadKey();
			}

			if (value.KeyChar.ToString().ToLower() == "n")
			{
				Console.WriteLine($"{Environment.NewLine}Task canceled. {Environment.NewLine}Press any key to exit...");
				Console.ReadKey();
				Environment.Exit(0);
				return;
			}

			// Add a line break, it looks better this way
			Console.WriteLine("");

			// Attempt to process all files
			processFiles(txtFiles);
		}

		// Attempt to process files that failed to process
		while (retryList.Count() > 0)
		{
			// Ask if the user want's to try and process failed files
			Console.Write($"{retryList.Count()} files were unable to be processed. Retry? (y/n) ");
			ConsoleKeyInfo refuseRetry = Console.ReadKey();

			while (refuseRetry.KeyChar.ToString().ToLower() != "n" && refuseRetry.KeyChar.ToString().ToLower() != "y")
			{
				Console.Write($"{Environment.NewLine}{retryList.Count()} files were unable to be processed. Retry? (y/n) ");
				refuseRetry = Console.ReadKey();
			}

			Console.WriteLine("");

			if (refuseRetry.KeyChar.ToString().ToLower() == "n")
			{
				// Save failed files to txt file
				File.WriteAllText(recoveredFileListPath, string.Join("\n", retryList.Select(item => item.Name)));

				// Log the failed files and exit the while loop
				log.Append($"{retryList.Count()} files were not processed and were saved to \"failedToProcessFiles.txt\".");
				Console.WriteLine($"{retryList.Count()} files were not processed and were saved to \"failedToProcessFiles.txt\".");
				break;
			}

			IEnumerable<SftpFile> retryListAsEnumerable = retryList.AsEnumerable();
			processFiles(retryListAsEnumerable);
		}

		// Disconnect from servers
		client1.Disconnect();
		client2.Disconnect();

		// Create the logs directory if it doesn't exist
		string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
		if (!Directory.Exists(logsDirectory)) Directory.CreateDirectory(logsDirectory);

		// Write the log data to disk
		File.WriteAllText(Path.Combine(logsDirectory, $"log-{DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss")}.txt"), log.ToString());

		Console.WriteLine($"Task Completed. {Environment.NewLine}Press any key to exit...");
		Console.ReadKey();
		Environment.Exit(0);
	}

	private static SftpClient connectToSFTPServer(string client)
	{
		// Skip this step if we're debugging
		if (System.Diagnostics.Debugger.IsAttached)
		{
			return new SftpClient("127.0.0.1", "username", "password");
		}

		// Wait for next keystroked
		Console.Write($"What is the server address for {client}? ");
		string? address = Console.ReadLine();
		Console.Write("Username: ");
		string? username = Console.ReadLine();
		Console.Write("Password: ");
		string password = hideKeyup();

		return new SftpClient(address, username, password);
	}

	private static string hideKeyup()
	{
		string password = "";

		while (true)
		{
			ConsoleKeyInfo key = Console.ReadKey(true);
			if (key.Key == ConsoleKey.Enter)
			{
				Console.WriteLine("");
				break; // Exit the loop when the user presses Enter
			}
			else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
			{
				password = password.Substring(0, password.Length - 1);
				Console.Write("\b \b"); // Erase the last character from the console
			}
			else if (char.IsLetterOrDigit(key.KeyChar))
			{
				password += key.KeyChar;
				Console.Write("*"); // Print a * to the console instead of the actual character
			}
		}

		return password;
	}

	private static void processFiles(IEnumerable<SftpFile> fileList)
	{
		// Empty the retry list
		retryList = new List<SftpFile>();

		foreach (SftpFile file in fileList)
		{
			try
			{
				Console.WriteLine($"Processing {file.Name}");

				// Attempt to transform the file
				MemoryStream mergedStream = transformFile(file);

				// Attempt to upload the file to server two
				client2.UploadFile(mergedStream, $"{clientTwoDirectoryLocation}/{file.Name}");

				log.Append($"Processed: {file.Name}. {Environment.NewLine}");
			}
			catch (Exception ex)
			{
				retryList.Add(file);
				Console.WriteLine($"Error: {ex.Message} | Unable to process {file.Name}.");
				log.Append($"Unable to process {file.Name}.{Environment.NewLine}Error: {ex.Message}");
			}
		}
	}

	private static MemoryStream transformFile(SftpFile file)
	{
		MemoryStream mergedStream = new MemoryStream();

		// TODO: rework this as it may not be as easy as this depending on the file format we are given.
		try
		{
			// Read file from first server
			MemoryStream fileStream = new MemoryStream();
			client1.DownloadFile($"{clientOneDirectoryLocation}/{file.Name}", fileStream);

			// Modify file, we are just adding "xxx" to the start and end of the txt file here because we don't know how we actually to modify the file
			var text = "xxx" + System.Text.Encoding.UTF8.GetString(fileStream.ToArray()) + "xxx";

			// Merge with new file
			var mergedText = text + "Hello World";

			// Write merged file to second server
			mergedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(mergedText));
		}
		catch (Exception ex)
		{
			retryList.Add(file);
			Console.WriteLine($"Error: {ex.Message} | Unable to process {file.Name}.");
			log.Append($"Unable to process {file.Name}.{Environment.NewLine}Error: {ex.Message}");
		}

		return mergedStream;
	}
}
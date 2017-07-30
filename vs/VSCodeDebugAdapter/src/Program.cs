/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace VSCodeDebugAdapter
{
	internal class Program
	{
		const int DEFAULT_PORT = 4711;

		private static bool trace_requests = true;
		private static bool trace_responses = true;
		static string LOG_FILE_PATH = null;

		private static void Main()
		{
            Program.Log("waiting for debug protocol on stdin/stdout");
            RunSession(Console.OpenStandardInput(), Console.OpenStandardOutput());
		}

		static TextWriter logFile;

		public static void Log(bool predicate, string format, params object[] data)
		{
			if (predicate)
			{
				Log(format, data);
			}
		}
		
		public static void Log(string format, params object[] data)
		{
			try
			{
				Console.Error.WriteLine(format, data);

				if (LOG_FILE_PATH != null)
				{
					if (logFile == null)
					{
						logFile = File.CreateText(LOG_FILE_PATH);
					}

					string msg = string.Format(format, data);
					logFile.WriteLine(string.Format("{0} {1}", DateTime.UtcNow.ToLongTimeString(), msg));
				}
			}
			catch (Exception ex)
			{
				if (LOG_FILE_PATH != null)
				{
					try
					{
						File.WriteAllText(LOG_FILE_PATH + ".err", ex.ToString());
					}
					catch
					{
					}
				}

				throw;
			}
		}

		private static void RunSession(Stream inputStream, Stream outputStream)
		{
			DebugSession debugSession = new SimpleDebugSession();
			debugSession.TRACE = trace_requests;
			debugSession.TRACE_RESPONSE = trace_responses;
			debugSession.Start(inputStream, outputStream).Wait();

			if (logFile!=null)
			{
				logFile.Flush();
				logFile.Close();
				logFile = null;
			}
		}

		private static void RunServer(int port)
		{
			TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
			serverSocket.Start();

			new System.Threading.Thread(() => {
				while (true) {
					var clientSocket = serverSocket.AcceptSocket();
					if (clientSocket != null) {
						Program.Log(">> accepted connection from client");

						new System.Threading.Thread(() => {
							using (var networkStream = new NetworkStream(clientSocket)) {
								try {
									RunSession(networkStream, networkStream);
								}
								catch (Exception e) {
									Console.Error.WriteLine("Exception: " + e);
								}
							}
							clientSocket.Close();
							Console.Error.WriteLine(">> client connection closed");
						}).Start();
					}
				}
			}).Start();
		}
	}
}

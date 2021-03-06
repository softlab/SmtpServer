﻿using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var options = new OptionsBuilder()
                .ServerName("SmtpServer SampleApp")
                .Port(9025)
                .MessageStore(new ConsoleMessageStore())
                .MailboxFilter(new ConsoleMailboxFilter())
                .Build();

            var serverTask = RunServerAsync(options, cancellationTokenSource.Token);
            var clientTask = RunClientAsync(cancellationTokenSource.Token);

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

            cancellationTokenSource.Cancel();

            serverTask.WaitWithoutException();
            clientTask.WaitWithoutException();
        }

        static async Task RunServerAsync(ISmtpServerOptions options, CancellationToken cancellationToken)
        {
            var smtpServer = new SmtpServer.SmtpServer(options);

            await smtpServer.StartAsync(cancellationToken);
        }

        static async Task RunClientAsync(CancellationToken cancellationToken)
        {
            var counter = 1;
            while (cancellationToken.IsCancellationRequested == false)
            {
                using (var smtpClient = new SmtpClient("localhost", 9025))
                {
                    try
                    {
                        await smtpClient.SendMailAsync(
                            new MailMessage("sample@test.com", "sample@test.com")
                            {
                                Subject = $"Message {counter++}"
                            });
                    }
                    catch (SmtpException smtpException)
                    {
                        Console.WriteLine(smtpException.StatusCode);
                    }
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}

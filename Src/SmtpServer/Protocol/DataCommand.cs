﻿using System;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer.Storage;

namespace SmtpServer.Protocol
{
    public sealed class DataCommand : SmtpCommand
    {
        readonly IMessageStore _messageStore;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageStore">The message store.</param>
        public DataCommand(IMessageStore messageStore)
        {
            if (messageStore == null)
            {
                throw new ArgumentNullException(nameof(messageStore));
            }

            _messageStore = messageStore;
        }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="context">The execution context to operate on.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task which asynchronously performs the execution.</returns>
        public override async Task ExecuteAsync(ISmtpSessionContext context, CancellationToken cancellationToken)
        {
            if (context.Transaction.To.Count == 0)
            {
                await context.Text.ReplyAsync(SmtpResponse.NoValidRecipientsGiven, cancellationToken).ConfigureAwait(false);
                return;
            }

            await context.Text.ReplyAsync(new SmtpResponse(SmtpReplyCode.StartMailInput, "end with <CRLF>.<CRLF>"), cancellationToken).ConfigureAwait(false);

            string text;
            while ((text = await context.Text.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != ".")
            {
                // need to trim the '.' at the start of the line if it 
                // exists as this would have been added for transparency
                // http://tools.ietf.org/html/rfc5321#section-4.5.2
                context.Transaction.Mime.AppendLine(text.TrimStart('.'));
            }

            try
            {
                // store the transaction
                var messageId = await _messageStore.SaveAsync(context.Transaction, cancellationToken).ConfigureAwait(false);

                await context.Text.ReplyAsync(new SmtpResponse(SmtpReplyCode.Ok, $"mail accepted ({messageId})"), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await context.Text.ReplyAsync(new SmtpResponse(SmtpReplyCode.MailboxUnavailable), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

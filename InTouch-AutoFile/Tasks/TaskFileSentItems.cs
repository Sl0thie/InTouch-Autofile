﻿namespace InTouch_AutoFile
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Outlook = Microsoft.Office.Interop.Outlook;
    using Serilog;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
    using Microsoft.Office.Interop.Outlook;

    internal class TaskFileSentItems
    {
        private readonly System.Action callBack;
        private readonly IList<Outlook.MailItem> mailToProcess = new List<Outlook.MailItem>();

        private IList<string> EntryIds = new List<string>();

        private string folderId;


        public TaskFileSentItems(System.Action callBack)
        {
            this.callBack = callBack;
        }

        public void RunTask()
        {
            //If task is enabled in the settings then start task.
            if (Properties.Settings.Default.TaskInbox)
            {
                Log.Information("Starting FileSent Task.");
                Thread backgroundThread = new Thread(new ThreadStart(BackgroundProcess))
                {
                    Name = "AF.FileSent",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal
                };
                backgroundThread.SetApartmentState(ApartmentState.STA);
                backgroundThread.Start();
            }
        }

        private void BackgroundProcess()
        {
            Thread.Sleep(10000);

            GetIds();
            ProcessEntryIds();
            callBack?.Invoke();
        }

        private void GetIds()
        {
            folderId = Globals.ThisAddIn.Application.GetNamespace("MAPI").GetDefaultFolder(Outlook.OlDefaultFolders.olFolderSentMail).StoreID;

            try
            {
                foreach (object nextItem in Globals.ThisAddIn.Application.GetNamespace("MAPI").GetDefaultFolder(Outlook.OlDefaultFolders.olFolderSentMail).Items)
                {
                    if (nextItem is Outlook.MailItem email)
                    {
                        //Only process emails that don't have a flag.
                        switch (email.FlagRequest)
                        {
                            case "":
                                EntryIds.Add(email.EntryID);
                                break;
                            case "Follow up":
                                //Don't process follow up. This leave them in the inbox for manual processing.
                                Log.Information("Move Email : Email has a flag set.");
                                break;
                            case null:
                                EntryIds.Add(email.EntryID);
                                break;
                            default:
                                Log.Information("Move Email : Unknown Flag Request Type.");
                                break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message, ex);
            }

        }

        private void ProcessEntryIds()
        {
            foreach (string nextItem in EntryIds)
            {
                ProcessEntry(nextItem);
            }
        }

        private void ProcessEntry(string entryId)
        {
            Outlook.MailItem email = null;

            try
            {
                email = (Outlook.MailItem)Globals.ThisAddIn.Application.GetNamespace("MAPI").GetItemFromID(entryId, folderId);

            }
            catch(System.Exception ex)
            {
                Log.Error(ex.Message, ex);
            }

            //Email may have been deleted or moved so check if it exists first.
            if (email is object)
            {
                //Check if the email has a Sender.
                if (email.Recipients is object)
                {
                    Outlook.Recipients recipients = email.Recipients;
                    Outlook.Recipient recipient = recipients[1];

                    if (recipient is object)
                    {
                        try
                        {
                            //Find the Contact associated with the Sender.
                            InTouchContact mailContact = null;
                            Outlook.ContactItem contact = InTouch.Contacts.FindContactFromEmailAddress(recipient.Address);
                            if (contact is object)
                            {
                                mailContact = new InTouchContact(contact);
                            }

                            //If found then try to process the email.
                            if (mailContact is object)
                            {

                                switch (mailContact.SentAction)
                                {
                                    case EmailAction.None: //Don't do anything to the email.
                                        Log.Information("Sent Email : Delivery Action set to None. " + recipient.Address);
                                        break;

                                    case EmailAction.Delete: //Delete the email if it is passed its action date.
                                        Log.Information("Sent Email : Deleting email from " + recipient.Address);
                                        email.Delete();
                                        break;

                                    case EmailAction.Move: //Move the email if its passed its action date.
                                        Log.Information("Sent Email : Moving email from " + recipient.Address);
                                        MoveEmailToFolder(mailContact.SentPath, email);
                                        break;
                                }
                                mailContact.SaveAndDispose();
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                            //throw;
                        }
                    }
                    else //If not found then just log it for the moment.
                    {
                        try
                        {
                            //Get the 'On Behalf' property from the email.
                            Outlook.PropertyAccessor mapiPropertyAccessor;
                            string propertyName = "http://schemas.microsoft.com/mapi/proptag/0x0065001F";
                            mapiPropertyAccessor = email.PropertyAccessor;
                            string onBehalfEmailAddress = mapiPropertyAccessor.GetProperty(propertyName).ToString();
                            if (mapiPropertyAccessor is object)
                            {
                                Marshal.ReleaseComObject(mapiPropertyAccessor);
                            }

                            //Log the details.                           
                            Log.Information("Sent Email : No Contact for " + email.SenderEmailAddress);
                            Log.Information("SenderName         : " + email.SenderName);
                            Log.Information("SentOnBehalfOfName : " + email.SentOnBehalfOfName);
                            Log.Information("ReplyRecipientNames: " + email.ReplyRecipientNames);
                            Log.Information("On Behalf: " + onBehalfEmailAddress);
                            Log.Information("");
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                            //throw;
                        }
                    }
                }
            }

            if (email is object)
            {
                Marshal.ReleaseComObject(email);
            }
        }

        private static void ProcessEmail(Outlook.MailItem email)
        {

            //Email may have been deleted or moved so check if it exists first.
            if (email is object)
            {
                //Check if the email has a Sender.
                if (email.Recipients is object)
                {
                    Outlook.Recipients recipients = email.Recipients;
                    Outlook.Recipient recipient = recipients[1];

                    if (recipient is object)
                    {
                        try
                        {
                            //Find the Contact associated with the Sender.
                            InTouchContact mailContact = null;
                            Outlook.ContactItem contact = InTouch.Contacts.FindContactFromEmailAddress(recipient.Address);
                            if (contact is object)
                            {
                                mailContact = new InTouchContact(contact);
                            }

                            //If found then try to process the email.
                            if (mailContact is object)
                            {

                                switch (mailContact.SentAction)
                                {
                                    case EmailAction.None: //Don't do anything to the email.
                                        Log.Information("Sent Email : Delivery Action set to None. " + recipient.Address);
                                        break;

                                    case EmailAction.Delete: //Delete the email if it is passed its action date.
                                        Log.Information("Sent Email : Deleting email from " + recipient.Address);
                                        email.Delete();
                                        break;

                                    case EmailAction.Move: //Move the email if its passed its action date.
                                        Log.Information("Sent Email : Moving email from " + recipient.Address);
                                        MoveEmailToFolder(mailContact.SentPath, email);
                                        break;
                                }
                                mailContact.SaveAndDispose();
                            }
                        }
                        catch(System.Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                            //throw;
                        }                   
                    }
                    else //If not found then just log it for the moment.
                    {
                        try
                        {
                            //Get the 'On Behalf' property from the email.
                            Outlook.PropertyAccessor mapiPropertyAccessor;
                            string propertyName = "http://schemas.microsoft.com/mapi/proptag/0x0065001F";
                            mapiPropertyAccessor = email.PropertyAccessor;
                            string onBehalfEmailAddress = mapiPropertyAccessor.GetProperty(propertyName).ToString();
                            if (mapiPropertyAccessor is object)
                            {
                                Marshal.ReleaseComObject(mapiPropertyAccessor);
                            }

                            //Log the details.                           
                            Log.Information("Sent Email : No Contact for " + email.SenderEmailAddress);
                            Log.Information("SenderName         : " + email.SenderName);
                            Log.Information("SentOnBehalfOfName : " + email.SentOnBehalfOfName);
                            Log.Information("ReplyRecipientNames: " + email.ReplyRecipientNames);
                            Log.Information("On Behalf: " + onBehalfEmailAddress);
                            Log.Information("");
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                            //throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to move the email from the Inbox to the specified folder.
        /// </summary>
        /// <param name="folderPath">The path to the folder to move the email.</param>
        /// <param name="email">The mailitem to move.</param>
        private static void MoveEmailToFolder(string folderPath, Outlook.MailItem email)
        {
            string[] folders = folderPath.Split('\\');
            Outlook.MAPIFolder folder;
            Outlook.Folders subFolders;

            try
            {
                folder = InTouch.Stores.StoresLookup[folders[0]].RootFolder;
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                Log.Information("Exception managed > Store not found. (" + folders[0] + ")");
                return;
            }

            try
            {
                for (int i = 1; i <= folders.GetUpperBound(0); i++)
                {
                    subFolders = folder.Folders;
                    folder = subFolders[folders[i]] as Outlook.Folder;
                }
            }
            catch (COMException ex)
            {
                if (ex.HResult == -2147221233)
                {
                    Log.Information("Exception Managed > Folder not found. (" + folderPath + ")");
                    return;
                }
                else
                {
                    throw;
                }
            }

            if (folder is object)
            {
                email.Move(folder);
                Marshal.ReleaseComObject(folder);
            }
        }
    }
}
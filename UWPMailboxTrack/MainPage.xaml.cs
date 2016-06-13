using RuntimeComponent1;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.UserDataAccounts;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPMailboxTrack
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += OnLoad;
        }

        private async void EmailTriggered(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    var md = new MessageDialog("email triggered");
                    await md.ShowAsync();
                });
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();

            Debug.WriteLine($"background execution status = {status}");

            // Register background task...
            var taskRegistered = false;
            var exampleTaskName = "MyEmailBackgroundTask";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == exampleTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (taskRegistered == false)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = nameof(EmailBackgroundTask);
                builder.TaskEntryPoint = typeof(EmailBackgroundTask).ToString();
                builder.SetTrigger(new EmailStoreNotificationTrigger());
                builder.Register().Completed += EmailTriggered;
            }

            UserDataAccountStore store =
                await UserDataAccountManager.RequestStoreAsync(UserDataAccountStoreAccessType.AllAccountsReadOnly);

            var accts = await store.FindAccountsAsync();
            foreach (var acct in accts)
            {
                foreach (EmailMailbox mailbox in await acct.FindEmailMailboxesAsync())
                {
                    //mailbox.MailboxChanged += Mailbox_MailboxChanged;
                    Debug.WriteLine($"mailbox id {mailbox.DisplayName}");
                }
            }
        }

        private async void Mailbox_MailboxChanged(EmailMailbox sender, EmailMailboxChangedEventArgs args)
        {
            try
            {
                Debug.WriteLine($"New Change Detected");
                var emailChangedDeferral = args.GetDeferral();
                if (sender.ChangeTracker.IsTracking == false)
                    sender.ChangeTracker.Enable();

                var changeReader = sender.ChangeTracker.GetChangeReader();
                var batch = await changeReader.ReadBatchAsync();

                Debug.WriteLine($"Num Changes in batch = {batch.Count}");
                foreach (var change in batch)
                {
                    Debug.WriteLine($"change type is {change.ChangeType.ToString()}");
                    foreach (var act in change.MailboxActions)
                    {
                        Debug.WriteLine($"kind = {act.Kind}");
                        Debug.WriteLine($"number = {act.ChangeNumber}");
                    }
                    if (change.Message != null)
                    {
                        Debug.WriteLine($"subject: {change.Message.Subject}");
                    }
                }

                changeReader.AcceptChanges();
                emailChangedDeferral.Complete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception {ex.Message}");
            }
        }
    }
}
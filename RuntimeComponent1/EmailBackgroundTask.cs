using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.UserDataAccounts;

namespace RuntimeComponent1
{
    public sealed class EmailBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("task running");

            BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();

            // Not using this...
            var details = (EmailStoreNotificationTriggerDetails)(taskInstance.TriggerDetails);

            try
            {
                UserDataAccountStore store =
                    await UserDataAccountManager.RequestStoreAsync(UserDataAccountStoreAccessType.AllAccountsReadOnly);

                var accts = await store.FindAccountsAsync();
                foreach (var acct in accts)
                {
                    foreach (EmailMailbox mailbox in await acct.FindEmailMailboxesAsync())
                    {
                        if (mailbox.ChangeTracker.IsTracking == false)
                            mailbox.ChangeTracker.Enable();

                        var changeReader = mailbox.ChangeTracker.GetChangeReader();
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
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"exception - {ex.Message}");
            }

            Debug.WriteLine("task triggered");
            _deferral.Complete();
        }
    }
}

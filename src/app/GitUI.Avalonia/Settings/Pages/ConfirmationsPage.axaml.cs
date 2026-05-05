using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class ConfirmationsPage : UserControl, ISettingsPage
{
    public ConfirmationsPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        ConfirmAmendCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmAmend", false);
        ConfirmUndoLastCommitCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmUndoLastCommit", false);
        ConfirmCommitIfNoBranchCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmCommitIfNoBranch", false);
        ConfirmRebaseCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmRebase", false);
        ConfirmFetchAndPruneAllCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmFetchAndPruneAll", false);
        ConfirmPushNewBranchCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmPushNewBranch", false);
        ConfirmAddTrackingRefCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmAddTrackingRef", false);
        ConfirmDeleteUnmergedBranchCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmDeleteUnmergedBranch", false);
        ConfirmBranchCheckoutCheckBox.IsChecked = App.Settings.GetBool("Confirmations.ConfirmBranchCheckout", false);
        ConfirmResolveConflictsCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmResolveConflicts", false);
        ConfirmCommitAfterConflictsCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmCommitAfterConflictsResolved", false);
        ConfirmSecondAbortCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmSecondAbortConfirmation", false);
        ConfirmSwitchWorktreeCheckBox.IsChecked = !App.Settings.GetBool("DontConfirmSwitchWorktree", false);
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("DontConfirmAmend", ConfirmAmendCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmUndoLastCommit", ConfirmUndoLastCommitCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmCommitIfNoBranch", ConfirmCommitIfNoBranchCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmRebase", ConfirmRebaseCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmFetchAndPruneAll", ConfirmFetchAndPruneAllCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmPushNewBranch", ConfirmPushNewBranchCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmAddTrackingRef", ConfirmAddTrackingRefCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmDeleteUnmergedBranch", ConfirmDeleteUnmergedBranchCheckBox.IsChecked != true);
        App.Settings.SetBool("Confirmations.ConfirmBranchCheckout", ConfirmBranchCheckoutCheckBox.IsChecked == true);
        App.Settings.SetBool("DontConfirmResolveConflicts", ConfirmResolveConflictsCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmCommitAfterConflictsResolved", ConfirmCommitAfterConflictsCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmSecondAbortConfirmation", ConfirmSecondAbortCheckBox.IsChecked != true);
        App.Settings.SetBool("DontConfirmSwitchWorktree", ConfirmSwitchWorktreeCheckBox.IsChecked != true);
    }
}

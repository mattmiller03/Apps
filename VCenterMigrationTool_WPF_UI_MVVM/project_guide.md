Project Guide: VCenterMigrationTool_WPF_UI
Welcome, AI agent!
This guide will help you understand, extend, and maintain the VCenterMigrationTool_WPF_UI WPF/MVVM project.

Project Purpose
This is a WPF application for managing vCenter migrations, featuring:

Profile management for source/destination vCenters
Secure credential handling
PowerShell-based connectivity/operations
Migration and validation task management
Activity logging
Project Structure
VCenterMigrationTool_WPF_UI/
│
├── Models/
│   ├── ConnectionProfile.cs
│   ├── ConnectionSettings.cs
│   ├── InventoryItemBase.cs
│   ├── DatacenterInfo.cs
│   ├── ClusterInfo.cs
│   ├── HostInfo.cs
│   ├── VMInfo.cs
│   └── (other domain models)
│
├── ViewModels/
│   ├── ConnectionSettingsViewModel.cs
│   ├── MigrationViewModel.cs
│   ├── ValidationViewModel.cs
│   ├── LogViewModel.cs
│   └── (other ViewModels)
│
├── Utilities/
│   ├── ConnectionManager.cs
│   ├── PowerShellManager.cs
│   └── (other helpers)
│
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ConnectionSettingsWindow.xaml
│   └── ConnectionSettingsWindow.xaml.cs
│
├── Resources/
│   ├── Styles.xaml
│   └── (other resource dictionaries)
│
├── App.xaml
├── App.xaml.cs
└── project_guide.md
Key Patterns & Conventions
MVVM: Business/UI logic belongs in ViewModels. Views/XAML handle UI layout and bindings only.
DataContext: Set in code-behind for MainWindow and major dialogs, not in XAML resources.
ViewModel Instantiation: ViewModels are created in code-behind and assigned to resources or as DataContext.
PasswordBox Handling: Passwords are relayed from the View via event handlers, not bound directly.
Profile Management: Profiles are stored in ConnectionProfile, with secure password encryption/decryption.
PowerShell Usage: All PowerShell interactions are encapsulated in PowerShellManager.
Resource Dictionaries: Shared styles (e.g., ModernButtonStyle) are in Resources/Styles.xaml and merged in App.xaml.
How To Extend
Add a New ViewModel: Derive from ObservableObject. Add [ObservableProperty] and [RelayCommand] as needed.
Add a New View/Dialog: Create the XAML in /Views. Set the DataContext in code-behind.
Add a Command or Property: Use the MVVM Toolkit attributes. For commands:
[RelayCommand]
public void DoSomething() { ... }
C#
Add/Update Styles: Place in Resources/Styles.xaml and merge into App.xaml.
Update Profile Logic: Edit ConnectionManager.cs and the ConnectionProfile/ConnectionSettings models.
Add PowerShell Features: Implement in PowerShellManager.cs. Expose new methods to ViewModels as needed.
Common Scenarios
1. Adding a New Profile
Add a ConnectionSettings object to Profiles in ConnectionSettingsViewModel.
Update the UI to bind and edit the selected profile.
2. Encrypt/Decrypt Passwords
Use static methods on ConnectionManager:
ConnectionManager.SaveConnectionProfilesAsync(profile)
ConnectionManager.LoadConnectionProfilesAsync()
3. Handling Passwords in the UI
In XAML, use:
<PasswordBox PasswordChanged="PasswordBox_PasswordChanged" />
XML
In code-behind:
private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    ViewModel.SelectedProfile.Password = ((PasswordBox)sender).Password;
}
C#
4. Adding a New PowerShell Operation
Implement in PowerShellManager.cs.
Expose via async methods.
Call from ViewModel command.
Troubleshooting & Conventions
Do NOT instantiate static classes (e.g., ConnectionManager).
Do NOT bind directly to PasswordBox.Password.
Do NOT use both Content property and inner content in XAML controls.
Do NOT define ViewModels in XAML resources; instantiate in code-behind.
Do use x:Key (not Key) for all XAML resources.
Do expose only public properties/commands you want to bind to in UI.
For AI Chat Agents
Always check if a ViewModel, Command, or Property already exists before adding.
Follow MVVM: business logic in ViewModel, UI-only logic in Views/code-behind.
When extending models, ensure constructors and property setters allow required usage (esp. for serialization/deserialization).
Prefer async/await for all I/O and PowerShell operations.
Update resource dictionaries when adding shared UI styles.
References
CommunityToolkit.Mvvm Documentation
WPF MVVM Best Practices – Microsoft Docs
For password handling, see official WPF Secure Password Patterns
Thank you for contributing! Please follow the patterns above, and always prefer maintainable, testable, and secure code.

Next Steps & Project Tracking
This section lists prioritized actions for ongoing development and improvement. Please update this as tasks are completed or added.

Current Top Priorities
Profile Management

 Complete implementations for creating, editing, deleting, and persisting connection profiles in the UI.
 Enable robust import/export for profile data.
PowerShell Integration

 Finalize migration/validation operations in PowerShellManager.
 Add error handling and reporting for PowerShell task failures.
 Optimize async operation handling and UI progress feedback.
Migration & Validation Workflows

 Build out the full migration pipeline in MigrationViewModel.
 Implement/expand validation tests and reporting in ValidationViewModel.
 Ensure logs capture all major user and system actions.
Security Enhancements

 Review and improve password encryption/decryption methods.
 Audit for secure password handling in memory and on disk.
User Experience

 Refine styles for accessibility and clarity (ModernButtonStyle, etc.).
 Add helpful tooltips and inline validation to forms.
 Ensure dialogs and error messages are clear and actionable.
Codebase Maintenance

 Add XML documentation and code comments to public members.
 Increase test coverage on critical logic (ConnectionManager, PowerShellManager).
 Refactor for interface-driven dependency injection where possible.
Upcoming/Backlog
 Implement VM/cluster selection tree and drag-and-drop migration planning.
 Add analytics for operation success/failure statistics.
 Support multi-language (localization/globalization) features.
 Package the app for reliable deployment (installer/bundling).
How to Contribute or Add Tasks
Add new tasks as - [ ] items under the appropriate section.
For each completed task, replace with - [x].
If tasks require further discussion, add a > NOTE: line beneath.
For every significant change, update this section and perform a code review.
Recent Activity
(Keep this up to date with recent changes for context!)

Refactored XAML to remove duplicate Content errors.
Resource dictionary for button styles added and referenced from App.xaml.
ViewModel instantiation and password relay pattern established in all dialogs.
Please check and update this section regularly to keep project progress visible and focused!
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress WPF XAML-related false positive errors
[assembly: SuppressMessage("Microsoft.Usage", "CS0103:The name does not exist in the current context", Justification = "WPF XAML generated code")]
[assembly: SuppressMessage("Microsoft.Usage", "CS1061:Type does not contain a definition", Justification = "WPF XAML generated code")]
[assembly: SuppressMessage("Microsoft.Usage", "CS0246:The type or namespace name could not be found", Justification = "WPF XAML generated code")]
[assembly: SuppressMessage("Microsoft.Usage", "CS0234:The type or namespace name does not exist in the namespace", Justification = "WPF XAML generated code")]
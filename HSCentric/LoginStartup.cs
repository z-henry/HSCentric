using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace HSCentric
{
    // The executable requires elevation, so use an interactive logon task rather
    // than a Run registry entry that cannot provide its administrator token.
    internal static class LoginStartup
    {
        private const string TaskName = "HSCentric-WebUI-17321";
        private const string Arguments = "--host=* --port=17321 --no-browser";
        internal const string AdministratorRequiredMessage = "后端当前未以管理员身份运行，无法修改开机启动。请在后端电脑上退出中控，右键 HSCentric.exe 选择“以管理员身份运行”，然后重新打开此设置。";
        private static string Executable => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HSCentric.exe");

        internal static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static bool IsEnabled()
        {
            using (var objects = new ComObjects())
            using (var identity = WindowsIdentity.GetCurrent())
            {
                dynamic service = objects.Keep(Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true)));
                service.Connect();
                dynamic folder = objects.Keep(service.GetFolder("\\"));
                dynamic task = FindTask(folder, objects);
                if (task == null || !task.Enabled) return false;
                dynamic definition = objects.Keep(task.Definition);
                dynamic principal = objects.Keep(definition.Principal);
                dynamic actions = objects.Keep(definition.Actions);
                dynamic triggers = objects.Keep(definition.Triggers);
                if (actions.Count != 1 || triggers.Count != 1 || principal.LogonType != 3) return false;
                dynamic action = objects.Keep(actions[1]);
                dynamic trigger = objects.Keep(triggers[1]);
                return action.Type == 0 && trigger.Type == 9 && trigger.Enabled &&
                    string.Equals((string)action.Path, Executable, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)action.Arguments, Arguments, StringComparison.Ordinal) &&
                    (string.Equals((string)principal.UserId, identity.User.Value, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals((string)principal.UserId, identity.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        internal static void SetEnabled(bool enabled)
        {
            if (!IsAdministrator()) throw new UnauthorizedAccessException(AdministratorRequiredMessage);
            using (var objects = new ComObjects())
            using (var identity = WindowsIdentity.GetCurrent())
            {
                dynamic service = objects.Keep(Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true)));
                service.Connect();
                dynamic folder = objects.Keep(service.GetFolder("\\"));
                if (!enabled)
                {
                    if (FindTask(folder, objects) != null) folder.DeleteTask(TaskName, 0);
                    return;
                }
                dynamic definition = objects.Keep(service.NewTask(0));
                dynamic registration = objects.Keep(definition.RegistrationInfo);
                registration.Description = "HSCentric：当前用户登录 Windows 后启动中控。";
                dynamic principal = objects.Keep(definition.Principal);
                principal.UserId = identity.User.Value;
                principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
                principal.RunLevel = 1; // TASK_RUNLEVEL_HIGHEST
                dynamic settings = objects.Keep(definition.Settings);
                settings.Enabled = true;
                settings.ExecutionTimeLimit = "PT0S";
                settings.MultipleInstances = 2; // TASK_INSTANCES_IGNORE_NEW
                settings.DisallowStartIfOnBatteries = false;
                settings.StopIfGoingOnBatteries = false;
                dynamic triggers = objects.Keep(definition.Triggers);
                dynamic trigger = objects.Keep(triggers.Create(9)); // TASK_TRIGGER_LOGON
                trigger.UserId = identity.User.Value;
                trigger.Enabled = true;
                dynamic actions = objects.Keep(definition.Actions);
                dynamic action = objects.Keep(actions.Create(0)); // TASK_ACTION_EXEC
                action.Path = Executable;
                action.Arguments = Arguments;
                action.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                objects.Keep(folder.RegisterTaskDefinition(TaskName, definition, 6, identity.User.Value, null, 3, null));
            }
        }

        private static dynamic FindTask(dynamic folder, ComObjects objects)
        {
            try { return objects.Keep(folder.GetTask(TaskName)); }
            // COM interop can translate missing-task HRESULTs to FileNotFoundException
            // or DirectoryNotFoundException instead of leaving them as COMException.
            // Only a missing task means disabled; permission/service errors must surface.
            catch (Exception ex) when (ex.HResult == unchecked((int)0x80070002) ||
                                       ex.HResult == unchecked((int)0x80070003)) { return null; }
        }

        private sealed class ComObjects : IDisposable
        {
            private readonly List<object> values = new List<object>();
            internal dynamic Keep(object value) { values.Add(value); return value; }
            public void Dispose()
            {
                for (int i = values.Count - 1; i >= 0; i--)
                    if (Marshal.IsComObject(values[i])) Marshal.ReleaseComObject(values[i]);
            }
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-3c（K9-1）: OSC 送信を有効にしたときの NullReferenceException の回帰テスト。
    /// <c>UseOscSender</c> が先に true になり、<c>oscSender</c> の生成（<c>ChangeOSCSenderStatus</c>）が終わる前に
    /// 入力スレッドが送信すると、アプリが未処理例外で終了していた（Step2 より前から存在した競合）。
    /// 送信側が未生成・破棄済みの間は、送信せずに通過することを固定する。
    /// </summary>
    public class ControlServiceOscSenderRaceTests
    {
        // テストプロジェクトは SharpOSC を参照していないため、UDPSender 型を直接使わず、リフレクションで読む
        private static object GetOscSender(ControlService service)
        {
            FieldInfo field = typeof(ControlService).GetField("oscSender",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return field.GetValue(service);
        }

        private static void InvokePrivate(ControlService service, string method, params object[] args)
        {
            MethodInfo info = typeof(ControlService).GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(info);
            try
            {
                info.Invoke(service, args);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        [Fact]
        public void OSCPreMappingStep_WhenSenderNotCreatedYet_DoesNotThrow()
        {
            ControlService service = ControlServiceTestFactory.Create();
            Assert.Null(GetOscSender(service));

            var cState = new DS4State { Battery = 50, Cross = true };
            var tempMapState = new DS4State();
            var oscMapState = new DS4State();

            // バッテリー値と Cross が異なるため、送信経路（SendOsc）を通る
            Exception ex = Record.Exception(() =>
                InvokePrivate(service, "OSCPreMappingStep", 0, cState, tempMapState, oscMapState));

            Assert.Null(ex);
        }

        [Fact]
        public void ChangeOSCSenderStatus_Disable_WhenSenderNotCreated_DoesNotThrow()
        {
            ControlService service = ControlServiceTestFactory.Create();
            Assert.Null(GetOscSender(service));

            Exception ex = Record.Exception(() => service.ChangeOSCSenderStatus(false));

            Assert.Null(ex);
            Assert.Null(GetOscSender(service));
        }

        [Fact]
        public void ControlServiceSource_HasNoDirectOscSenderSend()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (path == null) return;

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            // 送信はすべて SendOsc（null 安全）経由にする
            Assert.DoesNotContain("oscSender.Send(", text);
            Assert.Contains("private void SendOsc(OscMessage message)", text);
        }
    }
}

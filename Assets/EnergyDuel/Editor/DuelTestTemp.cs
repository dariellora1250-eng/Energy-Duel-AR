using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
internal class DuelTestTemp : ICallbacks
{
    [InitializeOnLoadMethod] static void Ready()=>EditorApplication.delayCall+=Run;
    static void Run()
    {
        if(!File.Exists("Temp/RunStageTests") || EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/RunStageTests");File.WriteAllText("Temp/DuelStageResults.txt","");
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();
        var reporter=new DuelTestTemp();api.RegisterCallbacks(reporter);
        try{api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{"EnergyDuel.Editor.DuelStageTests"}}){runSynchronously=true});}
        finally{api.UnregisterCallbacks(reporter);Object.DestroyImmediate(api);}
    }
    public void RunStarted(ITestAdaptor t){}
    public void TestStarted(ITestAdaptor t){}
    public void TestFinished(ITestResultAdaptor t){if(!t.HasChildren)File.AppendAllText("Temp/DuelStageResults.txt",t.FullName+" "+t.TestStatus+" "+t.Message+"\n");}
    public void RunFinished(ITestResultAdaptor t)=>File.AppendAllText("Temp/DuelStageResults.txt",$"passed={t.PassCount} failed={t.FailCount}\n");
} 

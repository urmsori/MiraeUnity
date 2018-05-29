using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirae;

namespace Mirae.Demo
{
    public class DemoMirae : MonoBehaviour
    {
        public Text connectText;
        public Text buildText;
        public Text runtimeText;
        public Text contextText;

        public void ConnectDoneHandler(bool success)
        {
            if (success)
                connectText.text = "Connected";
            else
                connectText.text = "Connect Failed";
        }
        public void BuildHandler(MiraeBuildResult result)
        {
            switch (result.Result)
            {
                case MiraeBuildError.Success:
                    buildText.text = "Build Done";
                    contextText.text = result.Context;
                    break;
                default:
                    buildText.text = result.Result.ToString();
                    break;
            }
        }
        public void RuntimeHandler(MiraeRuntimeResult result)
        {
            switch (result.Result)
            {
                case MiraeRuntimeResultType.Running:
                    buildText.text = "Run Once";
                    break;
                case MiraeRuntimeResultType.End:
                    buildText.text = "End";
                    break;
                case MiraeRuntimeResultType.Error:
                    buildText.text = result.Error.ToString();
                    break;
                default:
                    buildText.text = result.Result.ToString();
                    break;
            }
        }

        public bool ConditionCallback(object context)
        {
            var toggle = context as Toggle;
            return toggle.isOn;
        }
    }
}
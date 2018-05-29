using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirae.Demo
{
    [RequireComponent(typeof(Toggle))]
    public class DemoToggle : MonoBehaviour
    {
        public bool IsOn(object context)
        {
            return GetComponent<Toggle>().isOn;
        }

        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
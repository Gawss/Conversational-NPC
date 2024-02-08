using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Univrse.Demo.NPC
{
    public class Autorotate : MonoBehaviour
    {
        private float speed = 50f;

        // Update is called once per frame
        void Update()
        {
            transform.eulerAngles += Vector3.up * Time.deltaTime * speed;
        }
    }
}
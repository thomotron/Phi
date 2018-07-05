﻿using RimWorld;
using System;
using System.IO;
using UnityEngine;
using Verse;

namespace PhiClient
{
    public class PhiInitializer : ITab
    {
        public PhiInitializer()
        {
            // Initialise the first instance of the client
            PhiClient client = new PhiClient();
            client.Connect();

            // We use this as an entry to the main thread of the game.
            // Since the whole network layer receives messages in a different thread
            // than the game thread, we use this to resynchronize the whole thing.
            GameObject obj = new GameObject("Phi helper objects");
            
            obj.AddComponent<PhiComponent>();
        }

        protected override void FillTab()
        {
            throw new NotImplementedException();
        }
    }

    public class PhiComponent : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            PhiClient.instance.OnUpdate();
        }
    }
}

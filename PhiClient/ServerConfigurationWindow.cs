using PhiClient.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using WebSocketSharp;

// TODO: Uncomment and refactor

namespace PhiClient
{
    class ServerConfigurationWindow : Window
    {
        public ServerConfigurationWindow()
        {
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(700, 700);

        public override void PreOpen()
        {
            base.PreOpen();

            // Get a local copy of the current client instance
            PhiClient client = PhiClient.Instance;

            // Get the server address
            this.enteredAddress = client.ServerAddress;

//            if (client.IsUsable())
//            {
//                OnUsableCallback();
//            }
//            client.OnUsable += OnUsableCallback;
        }

        public override void PostClose()
        {
            base.PostClose();
            
//            PhiClient client = PhiClient.Instance;
//            client.OnUsable -= OnUsableCallback;
        }

        void OnUsableCallback()
        {
//            this.wantedNickname = PhiClient.instance.currentUser.name;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Get a local copy of the current client instance
            PhiClient instance = PhiClient.Instance;

            // Initialise a new container for the settings window
            ListContainer cont = new ListContainer
            {
                spaceBetween = ListContainer.SPACE
            };

            // Add the address box and connect/disconnect button
            cont.Add(new HeightContainer(DoHeader(instance), 30f));

            if (instance.IsLoggedIn)
            {
                // Add trade preferences and other server-specific elements
                cont.Add(DoConnectedContent(instance));
            }

            // Draw the window contents
            cont.Draw(inRect);
        }

        string enteredAddress = "";

        public Displayable DoHeader(PhiClient instance)
        {
            // Initialise a container for address box and connect/disconnect button
            ListContainer cont = new ListContainer(ListFlow.ROW)
            {
                spaceBetween = ListContainer.SPACE
            };
            
            if (instance.ConnectionState == WebSocketState.Open)
            {
                // Add a non-editable field displaying the currently connected address followed by a disconnect button
                cont.Add(new TextWidget("Connected to " + instance.ServerAddress, GameFont.Small, TextAnchor.MiddleLeft));
                cont.Add(new WidthContainer(new ButtonWidget("Disconnect", () => { OnDisconnectButtonClick(); }), 140f));
            }
            else
            {
                // Add an editable field with the last address and a connect button
                cont.Add(new TextFieldWidget(enteredAddress, (s) => { enteredAddress = s; }));
                cont.Add(new WidthContainer(new ButtonWidget("Connect", () => { OnConnectButtonClick(); }), 140f));
            }

            // Return the constructed header
            return cont;
        }

        string wantedNickname;

        public Displayable DoConnectedContent(PhiClient instance)
        {
            // Initialise a new container for server-specific content
            ListContainer mainCont = new ListContainer
            {
                spaceBetween = ListContainer.SPACE
            };

            // Initialise a container for nickname box and submit button
            ListContainer changeNickCont = new ListContainer(ListFlow.ROW)
            {
                spaceBetween = ListContainer.SPACE
            };

            // Add an editable nickname field and submit button to the container
            changeNickCont.Add(new TextFieldWidget(wantedNickname, null));
            changeNickCont.Add(new WidthContainer(new ButtonWidget("Change nickname", OnChangeNicknameClick), 140f));

            // Add the nickname container to the server-specific content container
            mainCont.Add(new HeightContainer(changeNickCont, 30f));


//            UserPreferences pref = client.currentUser.preferences;
//            ListContainer twoColumn = new ListContainer(ListFlow.ROW);
//            twoColumn.spaceBetween = ListContainer.SPACE;
//            mainCont.Add(twoColumn);
//
//            ListContainer firstColumn = new ListContainer();
//            twoColumn.Add(firstColumn);

//            firstColumn.Add(new CheckboxLabeledWidget("Allow receiving items", pref.receiveItems, (b) =>
//            {
//                pref.receiveItems = b;
//                client.UpdatePreferences();
//            }));
//
//            firstColumn.Add(new CheckboxLabeledWidget("Allow receiving colonists (EXPERIMENTAL)", pref.receiveColonists, (b) =>
//            {
//                pref.receiveColonists = b;
//                client.UpdatePreferences();
//            }));
//
//            firstColumn.Add(new CheckboxLabeledWidget("Allow receiving animals (EXPERIMENTAL)", pref.receiveAnimals, (b) =>
//            {
//                pref.receiveAnimals = b;
//                client.UpdatePreferences();
//            }));

//            // Just to take spaces while the column is empty
//            ListContainer secondColumn = new ListContainer();
//            twoColumn.Add(secondColumn);

            // Return the constructed container
            return mainCont;
        }

        public void OnConnectButtonClick()
        {
            PhiClient.Instance.ServerAddress = enteredAddress.Trim();
            PhiClient.Instance.Connect();
        }

        public void OnDisconnectButtonClick()
        {
            PhiClient.Instance.Disconnect();
        }

        void OnChangeNicknameClick()
        {
//            PhiClient.instance.ChangeNickname(wantedNickname);
        }
    }
}

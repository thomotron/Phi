using PhiClient.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WebSocketSharp;
using System;
using System.Runtime.Remoting.Messaging;

// TODO: Uncomment and refactor

namespace PhiClient
{
    public class ServerMainTab : MainTabWindow
    {
        const float TITLE_HEIGHT = 45f;
        const float CHAT_INPUT_HEIGHT = 30f;
        const float CHAT_INPUT_SEND_BUTTON_WIDTH = 100f;
        const float CHAT_MARGIN = 10f;
        const float STATUS_AREA_WIDTH = 160f;

        string enteredMessage = "";

        string filterName = "";
        List<Thing> filteredUsers;

        public override void DoWindowContents(Rect inRect)
        {
            // Draw the backing panel
            base.DoWindowContents(inRect);

            // Get a local copy of the current Phi instance
            PhiClient phi = PhiClient.Instance;

            // Initialise a container for the panel contents
            ListContainer mainList = new ListContainer
            {
                spaceBetween = ListContainer.SPACE
            };

            // Add a title to the panel content container
            mainList.Add(new TextWidget("Realm", GameFont.Medium, TextAnchor.MiddleCenter));

            // Initialise a body container for the chat panel and status column
            ListContainer rowBodyContainer = new ListContainer(new List<Displayable>
            {
                DoChat(phi),
                new WidthContainer(DoStatusArea(phi), STATUS_AREA_WIDTH)
            }, ListFlow.ROW);
            rowBodyContainer.spaceBetween = ListContainer.SPACE;

            // Add the body container to the panel content container
            mainList.Add(rowBodyContainer);

            // Add the chat entry bar to the panel content container
            mainList.Add(new HeightContainer(DoFooter(), 30f));

            // Draw the panel contents
            mainList.Draw(inRect);
        }

        Vector2 chatScroll = Vector2.zero;

        private Displayable DoChat(PhiClient instance)
        {
            // Initialise a container for chat messages
            var cont = new ListContainer(ListFlow.COLUMN, ListDirection.OPPOSITE);

            if (instance.IsLoggedIn) {
//                foreach (ChatMessage c in instance.realmData.chat.Reverse<ChatMessage>().Take(30))
//                {
//                    int idx = instance.realmData.users.LastIndexOf(c.user);
//                    cont.Add(new ButtonWidget(instance.realmData.users[idx].name + ": " + c.message, () => { OnUserClick(instance.realmData.users[idx]); }, false));
//                }
            }
            
            // Return a new chat list scrolled to the previously scrolled position
            // Any further scrolling will update the chatScroll property
            return new ScrollContainer(cont, chatScroll, (v) => { chatScroll = v; });
        }

        Vector2 userScrollPosition = Vector2.zero;

        private Displayable DoStatusArea(PhiClient instance)
        {
            // Initialise a new container for the column
            ListContainer cont = new ListContainer();
            cont.spaceBetween = ListContainer.SPACE;

            // Get the connection status
            string status = "Status: ";
            switch (instance.ConnectionState)
            {
                case WebSocketState.Open:
                    status += "Connected";
                    break;
                case WebSocketState.Closed:
                    status += "Disconnected";
                    break;
                case WebSocketState.Connecting:
                    status += "Connecting";
                    break;
                case WebSocketState.Closing:
                    status += "Disconnecting";
                    break;
            }

            // Add the status to the top of the column
            cont.Add(new TextWidget(status));

            // Add a configuration button to the column
            cont.Add(new HeightContainer(new ButtonWidget("Configuration", () => { OnConfigurationClick(); }), 30f));

            // Add a search bar to the column
            cont.Add(new Container(new TextFieldWidget(filterName, (s) => {
                filterName = s;
            }), 150f, 30f));

            if (instance.IsLoggedIn)
            {
                // Initialise a container for the user list
                ListContainer usersList = new ListContainer();
//                foreach (User user in instance.realmData.users.Where((u) => u.connected))
//                {
//                    if (filterName != "")
//                    {
//                        if (ContainsStringIgnoreCase(user.name, filterName))
//                            usersList.Add(new ButtonWidget(user.name, () => { OnUserClick(user); }, false));
//                    } else
//                    {
//                        usersList.Add(new ButtonWidget(user.name, () => { OnUserClick(user); }, false));
//                    }
//                }

                // Add the user list container to the column scrolled to the previously scrolled position
                // Any further scrolling will update the userScrollPosition property
                cont.Add(new ScrollContainer(usersList, userScrollPosition, (v) => { userScrollPosition = v; }));
            }

            // Return the assembled status area
            return cont;
        }

        private void OnConfigurationClick()
        {
            Find.WindowStack.Add(new ServerConfigurationWindow());
        }

        private bool ContainsStringIgnoreCase(string hay, string needle)
        {
            return hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Displayable DoFooter()
        {
            // Initialise a footer container
            ListContainer footerList = new ListContainer(ListFlow.ROW)
            {
                spaceBetween = ListContainer.SPACE
            };

            // Enter shortcut
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
            {
                OnSendClick();
                Event.current.Use();
            }

            // Add a textbox and send button
            footerList.Add(new TextFieldWidget(enteredMessage, s => enteredMessage = s));
            footerList.Add(new WidthContainer(new ButtonWidget("Send", OnSendClick), CHAT_INPUT_SEND_BUTTON_WIDTH));
            
            // Return the constructed footer
            return footerList;
        }

        public void OnSendClick()
        {
            string trimmedMessage = this.enteredMessage.Trim();
            if (trimmedMessage.IsNullOrEmpty())
            {
                return;
            }
//            PhiClient.instance.SendMessage(this.enteredMessage);
            this.enteredMessage = "";
        }

//        public void OnUserClick(User user)
//        {
//            PhiClient phiClient = PhiClient.instance;
//
//            if (user != phiClient.currentUser || true)
//            {
//                List<FloatMenuOption> options = new List<FloatMenuOption>();
//                options.Add(new FloatMenuOption("Ship items", () => { OnShipItemsOptionClick(user); }));
//                options.Add(new FloatMenuOption("Send colonist", () => { OnSendColonistOptionClick(user); }));
//                options.Add(new FloatMenuOption("Send animal", () => { OnSendAnimalOptionClick(user); }));
//
//                Find.WindowStack.Add(new FloatMenu(options));
//            }
//        }

//        public void OnSendColonistOptionClick(User user)
//        {
//            // We open a trade window with this user
//            if (user.preferences.receiveColonists)
//            {
//                Find.WindowStack.Add(new UserSendColonistWindow(user));
//            }
//            else
//            {
//                Messages.Message(user.name + " does not accept colonists", MessageTypeDefOf.RejectInput);
//            }
//        }

//        public void OnSendAnimalOptionClick(User user)
//        {
//            // We open a trade window with this user
//            if (user.preferences.receiveAnimals)
//            {
//                Find.WindowStack.Add(new UserSendAnimalWindow(user));
//            }
//            else
//            {
//                Messages.Message(user.name + " does not accept animals", MessageTypeDefOf.RejectInput);
//            }
//        }

//        public void OnShipItemsOptionClick(User user)
//        {
//            PhiClient phiClient = PhiClient.instance;
//            // We open a trade window with this user
//            if (user.preferences.receiveItems)
//            {
//                Find.WindowStack.Add(new UserGiveWindow(user));
//            }
//            else
//            {
//                Messages.Message(user.name + " does not accept items", MessageTypeDefOf.RejectInput);
//            }
//        }
    }
}

using DevionGames.UIWidgets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DevionGames.CharacterSystem
{
    public class CreateCharacterWindow : CharacterWindow
    {
        public override string[] Callbacks
        {
            get
            {
                List<string> callbacks = new List<string>(base.Callbacks);
                callbacks.Add("OnCharacterCreated");
                callbacks.Add("OnFailedToCreateCharacter");
                return callbacks.ToArray();
            }
        }

        [SerializeField]
        protected InputField m_CharacterName;
        [SerializeField]
        protected Button m_CreateButton;
        [SerializeField]
        protected Button m_BackButton;

        protected override void OnStart()
        {
            base.OnStart();
            ShowMale();
            this.m_CreateButton.onClick.AddListener(CreateCharacterUsingFields);
            this.m_BackButton.onClick.AddListener(delegate {
                UnityEngine.SceneManagement.SceneManager.LoadScene(CharacterManager.DefaultSettings.selectCharacterScene);
            });

            EventHandler.Register<Character>("OnCharacterCreated", OnCharacterCreated);
            EventHandler.Register<Character>("OnFailedToCreateCharacter", OnFailedToCreateCharacter);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventHandler.Unregister<Character>("OnCharacterCreated", OnCharacterCreated);
            EventHandler.Unregister<Character>("OnFailedToCreateCharacter", OnFailedToCreateCharacter);
        }

        /// <summary>
		/// Creates the character using referenced ui fields.
		/// </summary>
		public void CreateCharacterUsingFields()
        {
            if (string.IsNullOrEmpty(this.m_CharacterName.text))
            {
                CharacterManager.Notifications.nameIsEmpty.Show();
                return;
            }

            this.m_SelectedCharacter.CharacterName = this.m_CharacterName.text;

            // ✅ Find the bridge using GameObject.Find (works across assemblies)
            var bridgeObject = GameObject.Find("CharacterCreationBridge");
            if (bridgeObject != null)
            {
                string genderString = this.m_SelectedCharacter.Gender.ToString();

                // Use SendMessage to call across assemblies
                bridgeObject.SendMessage("CreateCharacterWithServer", new System.Collections.Generic.Dictionary<string, object>
        {
            {"characterName", this.m_CharacterName.text},
            {"className", this.m_SelectedCharacter.Name},
            {"genderString", genderString},
            {"characterTemplate", this.m_SelectedCharacter}
        });
            }
            else
            {
                Debug.LogError("[CreateCharacterWindow] CharacterCreationBridge GameObject not found! Creating character locally only.");
                // Fallback to original method
                CharacterManager.CreateCharacter(SelectedCharacter);
            }
        }

        private void OnCharacterCreated(Character character) {
            Execute("OnCharacterCreated", new CallbackEventData());
            UnityEngine.SceneManagement.SceneManager.LoadScene(CharacterManager.DefaultSettings.selectCharacterScene);
        }

        private void OnFailedToCreateCharacter(Character character) {
            Execute("OnFailedToCreateCharacter", new CallbackEventData());
            CharacterManager.Notifications.nameInUse.Show();
        }

        public void Show(Gender gender)
        {
            Clear();
            Character[] characters = CharacterManager.Database.items.Where(x => x.Gender == gender).ToArray();
            Add(characters);
            OnSelectionChange(Slots[0].ObservedCharacter);
        }

        public void ShowMale()
        {
            Show(Gender.Male);
        }

        public void ShowFemale()
        {
            Show(Gender.Female);
        }
    }
}
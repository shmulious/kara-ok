using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class URLItemsListView : MonoBehaviour
    {
        [SerializeField] private YouTubeURLListItemView _itemViewPrefab;
        public List<YouTubeURLListItemView> _listItems { get; private set; }
        [SerializeField] private Button _addItemButton; 

        private void Awake()
        {
            _addItemButton.onClick.AddListener(InstantiateListItem);
            _listItems = new List<YouTubeURLListItemView>();
        }

        private void InstantiateListItem()
        {
            var go = Instantiate(_itemViewPrefab, transform, true);
            go.transform.localScale = Vector3.one;
            go.RegisterOnRemove(RemoveItemFromList);
            go.RegisterOnProcess(OnProcessClicked);
            _listItems.Add(go);
            _addItemButton.transform.parent.SetAsLastSibling();
        }

        private void OnProcessClicked(YouTubeURLListItemView arg0, string arg1)
        {
            
        }

        private void RemoveItemFromList(YouTubeURLListItemView listItemView)
        {
            if (_listItems.Contains(listItemView))
            {
                _listItems.Remove(listItemView);
            }
            Destroy(listItemView.gameObject);
        }

        private void OnDestroy()
        {
            _addItemButton.onClick.RemoveAllListeners();
        }
    }
}
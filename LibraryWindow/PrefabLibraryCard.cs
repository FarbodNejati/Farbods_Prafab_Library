using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.PrefabLibrary
{
    public class PrefabLibraryCard : VisualElement
    {

        public static readonly string ussClassName = "library-item";
        public static readonly string selectedUssClassName = "active";
        public static readonly string previewUssClassName = ussClassName + "_preview";
        public static readonly string metaDataUssClassName = ussClassName + "_meta";
        public static readonly string textUssClassName = metaDataUssClassName + "__label";
        public static readonly string tagsContainerUssClassName = metaDataUssClassName + "__tags";
        public static readonly string tagUssClassName = metaDataUssClassName + "__tag";





        protected TextElement _TextElement;

        protected VisualElement _previewImageElement, _tagsContainer, _metaContainer;
        protected Texture2D _previewTexture;
        /// <summary>
        /// The current preview image assigned to this library item.
        /// </summary>
        public Texture2D PreviewTexture
        {
            get { return _previewTexture; }
            set
            {
                _previewTexture = value;
                if (_previewImageElement != null)
                    _previewImageElement.style.backgroundImage = new(value);
            }
        }

        string _text;
        /// <summary>
        /// The text displayed as the name of this library item.
        /// </summary>
        public string text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                _TextElement.text = value;
            }
        }
        public override VisualElement contentContainer => _metaContainer;
        public PrefabLibraryCard()
        {
            AddToClassList(ussClassName);

            //Preview Image
            _previewImageElement = new();
            _previewImageElement.AddToClassList(previewUssClassName);
            hierarchy.Add(_previewImageElement);

            //Wrapper for label and tags
            _metaContainer = new VisualElement();
            _metaContainer.AddToClassList(metaDataUssClassName);
            hierarchy.Add(_metaContainer);
            //Label
            _TextElement = new TextElement();
            _TextElement.AddToClassList(textUssClassName);
            _TextElement.pickingMode = PickingMode.Ignore;
            contentContainer.Add(_TextElement);

            //Tags
            _tagsContainer = new VisualElement();
            _tagsContainer.AddToClassList(tagsContainerUssClassName);
            contentContainer.Add(_tagsContainer);
        }
        public PrefabLibraryCard(string text, Texture2D image, LibraryTagData[] tags) : this()
        {
            _TextElement.text = text;
            PreviewTexture = image;
            SetTags(tags);
        }

        public virtual void SetTags(PrefabLibraryData data, List<LibraryTagData> tagRegistry)
        {
            if (data.tagGuids != null)
            {
                var tags = tagRegistry.Where(t => data.tagGuids.Contains(t.guid));
                SetTags(tags.ToArray());
            }
        }

        /// <summary>
        /// Add a list of tags, and set a class on them.
        /// Dict format : {Text, UssClass}
        /// </summary>
        /// <param name="tags"></param>
        public virtual void SetTags(LibraryTagData[] tags)
        {
            _tagsContainer.Clear();
            Array.ForEach(tags, t => AddTag(t));
        }



        public virtual void ClearTags() => _tagsContainer.Clear();

        public virtual VisualElement AddTag(LibraryTagData data)
        {
            Label tagLabel = new(data.name);
            tagLabel.name = data.guid;
            tagLabel.style.backgroundColor = data.color;
            tagLabel.AddToClassList(tagUssClassName);
            _tagsContainer.Add(tagLabel);

            return tagLabel;
        }
        public virtual void RemoveTag(string guid)
        {
            var target = _tagsContainer.Q(name: guid, className: tagUssClassName);
            if(target != null)
                _tagsContainer.Remove(target);
        }
    }
}
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;

namespace Dicom_Print_SCU.Configuration
{
    public class AeTitleConfigCollection : ConfigurationElementCollection, IEnumerable<AeTitleConfigElement>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public AeTitleConfigElement this[int index]
        {
            get
            {
                return BaseGet(index) as AeTitleConfigElement;
            }
        }

        public new IEnumerator<AeTitleConfigElement> GetEnumerator()
        {
            var count = Count;
            for (var i = 0; i < count; i++)
            {
                yield return BaseGet(i) as AeTitleConfigElement;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AeTitleConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AeTitleConfigElement)element).DisplayName;
        }
        public override bool IsReadOnly()
        {
            return false;
        }

        public void Add(AeTitleConfigElement aeTitleConfigElement)
        {
            BaseAdd(aeTitleConfigElement);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, aeTitleConfigElement));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public void InsertAt(AeTitleConfigElement aeTitleConfigElement, int index = 0)
        {
            BaseAdd(index, aeTitleConfigElement);
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, aeTitleConfigElement));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public void Clear()
        {
            BaseClear();

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public void Remove(AeTitleConfigElement aeTitleConfigElement)
        {
            int index = BaseIndexOf(aeTitleConfigElement);
            BaseRemove(aeTitleConfigElement.DisplayName);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, aeTitleConfigElement, index));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public void RemoveAt(int index)
        {
            ConfigurationElement element = BaseGet(index);

            BaseRemoveAt(index);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, element, index));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        public void Remove(string name)
        {
            ConfigurationElement element = BaseGet(name);
            int index = BaseIndexOf(element);

            BaseRemove(name);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, element, index));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }
    }
}

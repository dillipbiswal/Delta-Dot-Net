using System;
using System.Collections;
using System.Reflection;

namespace Datavail.Delta.Domain
{
    public abstract class DomainBase : ICloneable, IEntity, IDomainObject
    {
        #region Properties
        public virtual Guid Id { get; set; }
        #endregion

        #region ctor
        protected DomainBase()
        {
            Init();
        }

        protected void Init()
        {
            Id = Guid.NewGuid();
        }
        #endregion

        #region Methods
        public virtual bool CanDelete()
        {
            return true;
        }
        #endregion

        #region ICloneable Implementation
        /// <summary>
        /// Clone the object, and returning a reference to a cloned object.
        /// </summary>
        /// <returns>Reference to the new cloned 
        /// object.</returns>
        public virtual object Clone()
        {
            //First we create an instance of this specific type.
            object newObject = Activator.CreateInstance(this.GetType());

            //We get the array of fields for the new type instance.
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var typeProps = newObject.GetType().GetFields(flags);
            var baseProps = newObject.GetType().BaseType.GetFields(flags);

            var fields = new FieldInfo[typeProps.Length + baseProps.Length];
            typeProps.CopyTo(fields, 0);
            baseProps.CopyTo(fields, typeProps.Length);

            var i = 0;

            foreach (var fi in fields)
            {
                //We query if the fiels support the ICloneable interface.

                var cloneType = fi.FieldType.GetInterface("ICloneable", true);

                if (cloneType != null)
                {
                    //Getting the ICloneable interface from the object.
                    var clone = (ICloneable)fi.GetValue(this);

                    //We use the clone method to set the new value to the field.
                    fields[i].SetValue(newObject, clone != null ? clone.Clone() : null);
                }
                else
                {
                    // If the field doesn't support the ICloneable 
                    // interface then just set it.
                    fields[i].SetValue(newObject, fi.GetValue(this));
                }

                //Now we check if the object support the IEnumerable interface, so if it does
                //we need to enumerate all its items and check if they support the ICloneable interface.
                var enumerableType = fi.FieldType.GetInterface("IEnumerable", true);
                if (enumerableType != null)
                {
                    //Get the IEnumerable interface from the field.
                    var IEnum = (IEnumerable)fi.GetValue(this);

                    //This version support the IList and the IDictionary interfaces to iterate on collections.
                    var listType = fields[i].FieldType.GetInterface("IList", true);
                    var dicType = fields[i].FieldType.GetInterface("IDictionary", true);

                    var j = 0;
                    if (listType != null)
                    {
                        //Getting the IList interface.
                        var list = (IList)fields[i].GetValue(newObject);

                        foreach (var obj in IEnum)
                        {
                            //Checking to see if the current item support the ICloneable interface.
                            cloneType = obj.GetType().GetInterface("ICloneable", true);

                            if (cloneType != null)
                            {
                                //If it does support the ICloneable interface, 
                                //we use it to set the clone of
                                //the object in the list.
                                var clone = (ICloneable)obj;

                                list[j] = clone.Clone();
                            }

                            //NOTE: If the item in the list is not 
                            //support the ICloneable interface then in the 
                            //cloned list this item will be the same 
                            //item as in the original list
                            //(as long as this type is a reference type).
                            j++;
                        }
                    }
                    else if (dicType != null)
                    {
                        //Getting the dictionary interface.
                        var dic = (IDictionary)fields[i].GetValue(newObject);
                        j = 0;

                        foreach (DictionaryEntry de in IEnum)
                        {
                            //Checking to see if the item 
                            //support the ICloneable interface.
                            cloneType = de.Value.GetType().GetInterface("ICloneable", true);

                            if (cloneType != null)
                            {
                                var clone = (ICloneable)de.Value;

                                dic[de.Key] = clone.Clone();
                            }
                            j++;
                        }
                    }
                }
                i++;
            }
            return newObject;
        }
        #endregion
    }
}

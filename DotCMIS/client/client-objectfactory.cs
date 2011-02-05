﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotCMIS.Data;
using System.IO;
using DotCMIS.Enums;
using DotCMIS.Exceptions;
using System.Collections;

namespace DotCMIS.Client
{
    public class ObjectFactory : IObjectFactory
    {
        private ISession session;

        public void Initialize(ISession session, IDictionary<string, string> parameters)
        {
            this.session = session;
        }

        // ACL and ACE
        public IAcl ConvertAces(IList<IAce> aces)
        {
            if (aces == null) { return null; }

            Acl result = new Acl();
            result.Aces = new List<IAce>();

            foreach (IAce ace in aces)
            {
                result.Aces.Add(ace);
            }

            return result;
        }

        public IAcl CreateAcl(IList<IAce> aces)
        {
            Acl acl = new Acl();
            acl.Aces = aces;

            return acl;
        }

        public IAce CreateAce(string principal, IList<string> permissions)
        {
            Ace ace = new Ace();
            Principal acePrincipal = new Principal();
            acePrincipal.Id = principal;
            ace.Principal = acePrincipal;
            ace.Permissions = permissions;

            return ace;
        }

        // policies
        public IList<string> ConvertPolicies(IList<IPolicy> policies)
        {
            if (policies == null) { return null; }

            IList<string> result = new List<string>();
            foreach (IPolicy policy in policies)
            {
                if (policy != null && policy.Id != null)
                {
                    result.Add(policy.Id);
                }
            }

            return result;
        }

        // renditions
        public IRendition ConvertRendition(string objectId, IRenditionData rendition)
        {
            if (rendition == null)
            {
                throw new ArgumentException("rendition");
            }

            return new Rendition(this.session, objectId, rendition.StreamId, rendition.MimeType, rendition.Length, rendition.Kind,
                      rendition.Title, rendition.Height, rendition.Height, rendition.RenditionDocumentId);
        }

        // content stream
        public IContentStream CreateContentStream(string filename, long length, string mimetype, Stream stream)
        {
            ContentStream result = new ContentStream();
            result.FileName = filename;
            result.Length = length;
            result.MimeType = mimetype;
            result.Stream = stream;

            return result;
        }

        // types
        public IObjectType ConvertTypeDefinition(ITypeDefinition typeDefinition)
        {
            switch (typeDefinition.BaseTypeId)
            {
                case BaseTypeId.CmisDocument:
                    return new DocumentType(session, (IDocumentTypeDefinition)typeDefinition);
                case BaseTypeId.CmisFolder:
                    return new FolderType(session, (IFolderTypeDefinition)typeDefinition);
                case BaseTypeId.CmisRelationship:
                    return new RelationshipType(session, (IRelationshipTypeDefinition)typeDefinition);
                case BaseTypeId.CmisPolicy:
                    return new PolicyType(session, (IPolicyTypeDefinition)typeDefinition);
                default:
                    throw new CmisRuntimeException("Unknown base type!");
            }
        }

        public IObjectType GetTypeFromObjectData(IObjectData objectData)
        {
            if (objectData == null || objectData.Properties == null)
            {
                return null;
            }

            IPropertyId typeProperty = objectData.Properties[PropertyIds.ObjectTypeId] as IPropertyId;
            if (typeProperty == null)
            {
                return null;
            }

            return session.GetTypeDefinition(typeProperty.FirstValue);
        }

        // properties
        public IProperty CreateProperty<T>(IPropertyDefinition type, IList<T> values)
        {
            return new Property(type, (IList<object>)values);
        }

        protected IProperty ConvertProperty(IObjectType objectType, IPropertyData pd)
        {
            IPropertyDefinition definition = objectType[pd.Id];
            if (definition == null)
            {
                // property without definition
                throw new CmisRuntimeException("Property '" + pd.Id + "' doesn't exist!");
            }

            switch (definition.PropertyType)
            {
                case PropertyType.String:
                    return CreateProperty(definition, ((IPropertyString)pd).Values);
                case PropertyType.Id:
                    return CreateProperty(definition, ((IPropertyId)pd).Values);
                case PropertyType.Integer:
                    return CreateProperty(definition, ((IPropertyInteger)pd).Values);
                case PropertyType.Boolean:
                    return CreateProperty(definition, ((IPropertyBoolean)pd).Values);
                case PropertyType.Decimal:
                    return CreateProperty(definition, ((IPropertyDecimal)pd).Values);
                case PropertyType.Uri:
                    return CreateProperty(definition, ((IPropertyUri)pd).Values);
                case PropertyType.Html:
                    return CreateProperty(definition, ((IPropertyHtml)pd).Values);
                default:
                    return null;
            }
        }

        public IDictionary<string, IProperty> ConvertProperties(IObjectType objectType, IProperties properties)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException("objectType");
            }

            if (objectType.PropertyDefintions == null)
            {
                throw new ArgumentException("Object type has no property defintions!");
            }

            if (properties == null || properties.PropertyList == null)
            {
                throw new ArgumentException("Properties must be set!");
            }

            // iterate through properties and convert them
            IDictionary<string, IProperty> result = new Dictionary<string, IProperty>();
            foreach (IPropertyData property in properties.PropertyList)
            {
                // find property definition
                IProperty apiProperty = ConvertProperty(objectType, property);
                result[property.Id] = apiProperty;
            }

            return result;
        }

        public IProperties ConvertProperties(IDictionary<string, object> properties, IObjectType type, HashSet<Updatability> updatabilityFilter)
        {
            // check input
            if (properties == null)
            {
                return null;
            }

            // get the type
            if (type == null)
            {
                string typeId = properties[PropertyIds.ObjectTypeId] as string;
                if (typeId == null)
                {
                    throw new ArgumentException("Type or type property must be set!");
                }

                type = session.GetTypeDefinition(typeId);
            }

            Properties result = new Properties();

            // the big loop
            foreach (KeyValuePair<string, object> property in properties)
            {
                string id = property.Key;
                object value = property.Value;

                if (value is IProperty)
                {
                    IProperty p = (IProperty)value;
                    if (id != p.Id)
                    {
                        throw new ArgumentException("Property id mismatch: '" + id + "' != '" + p.Id + "'!");
                    }
                    value = (p.PropertyDefinition.Cardinality == Cardinality.Single ? p.FirstValue : p.Values);
                }

                // get the property definition
                IPropertyDefinition definition = type[id];
                if (definition == null)
                {
                    throw new ArgumentException("Property +'" + id + "' is not valid for this type!");
                }

                // check updatability
                if (updatabilityFilter != null)
                {
                    if (!updatabilityFilter.Contains(definition.Updatability))
                    {
                        continue;
                    }
                }

                // single and multi value check
                IList<dynamic> values;
                if (value == null)
                {
                    values = null;
                }
                else if (value is IList<dynamic>)
                {
                    if (definition.Cardinality != Cardinality.Multi)
                    {
                        throw new ArgumentException("Property '" + id + "' is not a multi value property!");
                    }
                    values = (IList<dynamic>)value;

                    // check if the list is homogeneous and does not contain null values
                    Type valueType = null;
                    foreach (object o in values)
                    {
                        if (o == null)
                        {
                            throw new ArgumentException("Property '" + id + "' contains null values!");
                        }
                        if (valueType == null)
                        {
                            valueType = o.GetType();
                        }
                        else
                        {
                            if (!valueType.IsInstanceOfType(o))
                            {
                                throw new ArgumentException("Property '" + id + "' is inhomogeneous!");
                            }
                        }
                    }
                }
                else
                {
                    if (definition.Cardinality != Cardinality.Single)
                    {
                        throw new ArgumentException("Property '" + id + "' is not a single value property!");
                    }
                    values = new List<dynamic>();
                    values.Add(value);
                }

                // assemble property
                object firstValue = (values == null || values.Count == 0 ? null : values[0]);

                switch (definition.PropertyType)
                {
                    case PropertyType.String:
                        PropertyString stringProperty = new PropertyString();
                        stringProperty.Id = id;
                        stringProperty.Values = new List<string>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is string))
                            {
                                throw new ArgumentException("Property '" + id + "' is a String property!");
                            }

                            foreach (object o in values)
                            {
                                stringProperty.Values.Add((string)o);
                            }
                        }
                        result.AddProperty(stringProperty);
                        break;
                    case PropertyType.Id:
                        PropertyId idProperty = new PropertyId();
                        idProperty.Id = id;
                        idProperty.Values = new List<string>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is string))
                            {
                                throw new ArgumentException("Property '" + id + "' is a Id property!");
                            }

                            foreach (object o in values)
                            {
                                idProperty.Values.Add((string)o);
                            }
                        }
                        result.AddProperty(idProperty);
                        break;
                    case PropertyType.Integer:
                        PropertyInteger intProperty = new PropertyInteger();
                        intProperty.Id = id;
                        intProperty.Values = new List<long>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is sbyte || firstValue is byte || firstValue is short || firstValue is ushort || firstValue is int || firstValue is uint || firstValue is long))
                            {
                                throw new ArgumentException("Property '" + id + "' is an Integer property!");
                            }

                            foreach (object o in values)
                            {
                                intProperty.Values.Add((long)o);
                            }
                        }
                        result.AddProperty(intProperty);
                        break;
                    case PropertyType.Boolean:
                        PropertyBoolean booleanProperty = new PropertyBoolean();
                        booleanProperty.Id = id;
                        booleanProperty.Values = new List<bool>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is bool))
                            {
                                throw new ArgumentException("Property '" + id + "' is a boolean property!");
                            }

                            foreach (object o in values)
                            {
                                booleanProperty.Values.Add((bool)o);
                            }
                        }
                        result.AddProperty(booleanProperty);
                        break;
                    case PropertyType.DateTime:
                        PropertyDateTime dateTimeProperty = new PropertyDateTime();
                        dateTimeProperty.Id = id;
                        dateTimeProperty.Values = new List<DateTime>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is DateTime))
                            {
                                throw new ArgumentException("Property '" + id + "' is a Boolean property!");
                            }

                            foreach (object o in values)
                            {
                                dateTimeProperty.Values.Add((DateTime)o);
                            }
                        }
                        result.AddProperty(dateTimeProperty);
                        break;
                    case PropertyType.Decimal:
                        PropertyDecimal decimalProperty = new PropertyDecimal();
                        decimalProperty.Id = id;
                        decimalProperty.Values = new List<decimal>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is DateTime))
                            {
                                throw new ArgumentException("Property '" + id + "' is a Decimal property!");
                            }

                            foreach (object o in values)
                            {
                                decimalProperty.Values.Add((decimal)o);
                            }
                        }
                        result.AddProperty(decimalProperty);
                        break;
                    case PropertyType.Uri:
                        PropertyUri uriProperty = new PropertyUri();
                        uriProperty.Id = id;
                        uriProperty.Values = new List<string>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is string))
                            {
                                throw new ArgumentException("Property '" + id + "' is a URI property!");
                            }

                            foreach (object o in values)
                            {
                                uriProperty.Values.Add((string)o);
                            }
                        }
                        result.AddProperty(uriProperty);
                        break;
                    case PropertyType.Html:
                        PropertyHtml htmlProperty = new PropertyHtml();
                        htmlProperty.Id = id;
                        htmlProperty.Values = new List<string>(values.Count);
                        if (values != null)
                        {
                            if (firstValue != null && !(firstValue is string))
                            {
                                throw new ArgumentException("Property '" + id + "' is a HTML property!");
                            }

                            foreach (object o in values)
                            {
                                htmlProperty.Values.Add((string)o);
                            }
                        }
                        result.AddProperty(htmlProperty);
                        break;
                }
            }

            return result;
        }

        public IList<IPropertyData> ConvertQueryProperties(IProperties properties)
        {
            if ((properties == null) || (properties.PropertyList == null))
            {
                throw new ArgumentException("Properties must be set!");
            }

            return new List<IPropertyData>(properties.PropertyList);
        }

        // objects
        public ICmisObject ConvertObject(IObjectData objectData, IOperationContext context)
        {
            if (objectData == null)
            {
                throw new ArgumentNullException("objectData");
            }

            IObjectType type = GetTypeFromObjectData(objectData);

            switch (objectData.BaseTypeId)
            {
                case BaseTypeId.CmisDocument:
                    return new Document(session, type, objectData, context);
                case BaseTypeId.CmisFolder:
                    return new Folder(session, type, objectData, context);
                case BaseTypeId.CmisPolicy:
                    return new Policy(session, type, objectData, context);
                case BaseTypeId.CmisRelationship:
                    return new Relationship(session, type, objectData, context);
                default:
                    throw new CmisRuntimeException("Unsupported type: " + objectData.BaseTypeId);
            }
        }

        public IQueryResult ConvertQueryResult(IObjectData objectData) { return null; }
        public IChangeEvent ConvertChangeEvent(IObjectData objectData) { return null; }
        public IChangeEvents ConvertChangeEvents(String changeLogToken, IObjectList objectList) { return null; }
    }
}

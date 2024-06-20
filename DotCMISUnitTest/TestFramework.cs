/*
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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using DotCMIS;
using DotCMIS.Binding;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data;
using DotCMIS.Data.Impl;
using DotCMIS.Enums;
using DotCMIS.Exceptions;
using NUnit.Framework;

namespace DotCMISUnitTest
{
    public class TestFramework
    {
        private IRepositoryInfo repositoryInfo;

        public ISession Session { get; set; }
        public ICmisBinding Binding { get { return Session.Binding; } }
        public IRepositoryInfo RepositoryInfo
        {
            get
            {
                if (repositoryInfo == null)
                {
                    repositoryInfo = Binding.GetRepositoryService().GetRepositoryInfos(null)[0];
                    Assert.That(repositoryInfo, Is.Not.Null);
                }

                return repositoryInfo;
            }
        }

        public string DefaultDocumentType { get; set; }
        public string DefaultFolderType { get; set; }
        public IFolder TestFolder { get; set; }

        [SetUp]
        public void Init()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            DefaultDocumentType = "cmis:document";
            DefaultFolderType = "cmis:folder";

            Session = ConnectFromConfig();
        }

        public ISession ConnectFromConfig()
        {
            Dictionary<string, string> parameters = AppSettingsHelper.GetDictionaryAppSettings();

            string documentType = AppSettingsHelper.GetAppSetting("test.documenttype");
            if (documentType != null)
            {
                DefaultDocumentType = documentType;
            }

            string folderType = AppSettingsHelper.GetAppSetting("test.foldertype");
            if (folderType != null)
            {
                DefaultFolderType = folderType;
            }

            SessionFactory factory = SessionFactory.NewInstance();

            ISession session = null;
            if (parameters.ContainsKey(SessionParameter.RepositoryId))
            {
                session = factory.CreateSession(parameters);
            }
            else
            {
                session = factory.GetRepositories(parameters)[0].CreateSession();
            }

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Binding, Is.Not.Null);
            Assert.That(session.RepositoryInfo, Is.Not.Null);
            Assert.That(session.RepositoryInfo.Id, Is.Not.Null);

            string testRootFolderPath = AppSettingsHelper.GetAppSetting("test.rootfolder");
            if (testRootFolderPath == null)
            {
                TestFolder = session.GetRootFolder();
            }
            else
            {
                TestFolder = session.GetObjectByPath(testRootFolderPath) as IFolder;
            }

            Assert.That(TestFolder, Is.Not.Null);
            Assert.That(TestFolder.Id, Is.Not.Null);

            return session;
        }

        public ISession ConnectAtomPub()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters[SessionParameter.BindingType] = BindingType.AtomPub;
            parameters[SessionParameter.AtomPubUrl] = AppSettingsHelper.GetAppSetting(SessionParameter.AtomPubUrl);
            parameters[SessionParameter.User] = AppSettingsHelper.GetAppSetting(SessionParameter.User);
            parameters[SessionParameter.Password] = AppSettingsHelper.GetAppSetting(SessionParameter.Password);

            SessionFactory factory = SessionFactory.NewInstance();
            ISession session = factory.GetRepositories(parameters)[0].CreateSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Binding, Is.Not.Null);
            Assert.That(session.RepositoryInfo, Is.Not.Null);
            Assert.That(session.RepositoryInfo.Id, Is.Not.Null);

            return session;
        }

        public ISession ConnectWebServices()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            string baseUrlWS = "https://localhost:8443/alfresco/cmisws";

            parameters[SessionParameter.BindingType] = BindingType.WebServices;
            parameters[SessionParameter.WebServicesRepositoryService] = baseUrlWS + "/RepositoryService?wsdl";
            parameters[SessionParameter.WebServicesAclService] = baseUrlWS + "/AclService?wsdl";
            parameters[SessionParameter.WebServicesDiscoveryService] = baseUrlWS + "/DiscoveryService?wsdl";
            parameters[SessionParameter.WebServicesMultifilingService] = baseUrlWS + "/MultifilingService?wsdl";
            parameters[SessionParameter.WebServicesNavigationService] = baseUrlWS + "/NavigationService?wsdl";
            parameters[SessionParameter.WebServicesObjectService] = baseUrlWS + "/ObjectService?wsdl";
            parameters[SessionParameter.WebServicesPolicyService] = baseUrlWS + "/PolicyService?wsdl";
            parameters[SessionParameter.WebServicesRelationshipService] = baseUrlWS + "/RelationshipService?wsdl";
            parameters[SessionParameter.WebServicesVersioningService] = baseUrlWS + "/VersioningService?wsdl";
            parameters[SessionParameter.User] = "admin";
            parameters[SessionParameter.Password] = "admin";

            SessionFactory factory = SessionFactory.NewInstance();
            ISession session = factory.GetRepositories(parameters)[0].CreateSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(session.Binding, Is.Not.Null);
            Assert.That(session.RepositoryInfo, Is.Not.Null);
            Assert.That(session.RepositoryInfo.Id, Is.Not.Null);

            return session;
        }

        public IObjectData GetFullObject(string objectId)
        {
            IObjectData result = Binding.GetObjectService().GetObject(RepositoryInfo.Id, objectId, "*", true, IncludeRelationshipsFlag.Both, "*", true, true, null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.Not.Null);
            Assert.That(result.BaseTypeId, Is.Not.Null);
            Assert.That(result.Properties, Is.Not.Null);

            return result;
        }

        public IObjectData CreateDocument(string folderId, string name, string content)
        {
            DotCMIS.Data.Impl.Properties properties = new DotCMIS.Data.Impl.Properties();

            PropertyData objectTypeIdProperty = new PropertyData(PropertyType.Id);
            objectTypeIdProperty.Id = PropertyIds.ObjectTypeId;
            objectTypeIdProperty.Values = new List<object>();
            objectTypeIdProperty.Values.Add(DefaultDocumentType);
            properties.AddProperty(objectTypeIdProperty);

            PropertyData nameProperty = new PropertyData(PropertyType.String);
            nameProperty.Id = PropertyIds.Name;
            nameProperty.Values = new List<object>();
            nameProperty.Values.Add(name);
            properties.AddProperty(nameProperty);

            ContentStream contentStream = null;
            if (content != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);

                contentStream = new ContentStream();
                contentStream.FileName = name;
                contentStream.MimeType = "text/plain";
                contentStream.Stream = new MemoryStream(bytes);
                contentStream.Length = bytes.Length;
            }

            string newDocId = Binding.GetObjectService().CreateDocument(RepositoryInfo.Id, properties, folderId, contentStream, null, null, null, null, null);

            Assert.That(newDocId, Is.Not.Null);

            IObjectData doc = GetFullObject(newDocId);

            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Id, Is.Not.Null);
            Assert.That(doc.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));
            Assert.That(doc.Properties, Is.Not.Null);
            Assert.That(doc.Properties[PropertyIds.ObjectTypeId], Is.Not.Null);
            Assert.That(doc.Properties[PropertyIds.ObjectTypeId].PropertyType, Is.EqualTo(PropertyType.Id));
            Assert.That(doc.Properties[PropertyIds.ObjectTypeId].FirstValue as string, Is.EqualTo(DefaultDocumentType));
            Assert.That(doc.Properties[PropertyIds.Name], Is.Not.Null);
            Assert.That(doc.Properties[PropertyIds.Name].PropertyType, Is.EqualTo(PropertyType.String));
            Assert.That(doc.Properties[PropertyIds.Name].FirstValue as string, Is.EqualTo(name));

            if (folderId != null)
            {
                CheckObjectInFolder(newDocId, folderId);
            }

            return doc;
        }

        public IObjectData CreateFolder(string folderId, string name)
        {
            DotCMIS.Data.Impl.Properties properties = new DotCMIS.Data.Impl.Properties();

            PropertyData objectTypeIdProperty = new PropertyData(PropertyType.Id);
            objectTypeIdProperty.Id = PropertyIds.ObjectTypeId;
            objectTypeIdProperty.Values = new List<object>();
            objectTypeIdProperty.Values.Add(DefaultFolderType);
            properties.AddProperty(objectTypeIdProperty);

            PropertyData nameProperty = new PropertyData(PropertyType.String);
            nameProperty.Id = PropertyIds.Name;
            nameProperty.Values = new List<object>();
            nameProperty.Values.Add(name);
            properties.AddProperty(nameProperty);

            string newFolderId = Binding.GetObjectService().CreateFolder(RepositoryInfo.Id, properties, folderId, null, null, null, null);

            Assert.That(newFolderId, Is.Not.Null);

            IObjectData folder = GetFullObject(newFolderId);

            Assert.That(folder, Is.Not.Null);
            Assert.That(folder.Id, Is.Not.Null);
            Assert.That(folder.BaseTypeId, Is.EqualTo(BaseTypeId.CmisFolder));
            Assert.That(folder.Properties, Is.Not.Null);
            Assert.That(folder.Properties[PropertyIds.ObjectTypeId], Is.Not.Null);
            Assert.That(folder.Properties[PropertyIds.ObjectTypeId].PropertyType, Is.EqualTo(PropertyType.Id));
            Assert.That(folder.Properties[PropertyIds.ObjectTypeId].FirstValue as string, Is.EqualTo(DefaultFolderType));
            Assert.That(folder.Properties[PropertyIds.Name], Is.Not.Null);
            Assert.That(folder.Properties[PropertyIds.Name].PropertyType, Is.EqualTo(PropertyType.String));
            Assert.That(folder.Properties[PropertyIds.Name].FirstValue as string, Is.EqualTo(name));

            if (folderId != null)
            {
                CheckObjectInFolder(newFolderId, folderId);
            }

            return folder;
        }

        public void CheckObjectInFolder(string objectId, string folderId)
        {
            // check parents
            IList<IObjectParentData> parents = Binding.GetNavigationService().GetObjectParents(RepositoryInfo.Id, objectId, null, null, null, null, null, null);

            Assert.That(parents, Is.Not.Null);
            Assert.That(parents.Count > 0, Is.True);

            bool found = false;
            foreach (IObjectParentData parent in parents)
            {
                Assert.That(parent, Is.Not.Null);
                Assert.That(parent.Object, Is.Not.Null);
                Assert.That(parent.Object.Id, Is.Not.Null);
                if (parent.Object.Id == folderId)
                {
                    found = true;
                }
            }
            Assert.That(found, Is.True);

            // check children
            found = false;
            bool hasMore = true;
            long maxItems = 100;
            long skipCount = 0;

            while (hasMore)
            {
                IObjectInFolderList children = Binding.GetNavigationService().GetChildren(RepositoryInfo.Id, folderId, null, null, null, null, null, null, null, maxItems, skipCount, null);

                Assert.That(children, Is.Not.Null);
                if (children.NumItems != null)
                {
                    Assert.That(children.NumItems > 0, Is.True);
                }

                foreach (ObjectInFolderData obj in children.Objects)
                {
                    Assert.That(obj, Is.Not.Null);
                    Assert.That(obj.Object, Is.Not.Null);
                    Assert.That(obj.Object.Id, Is.Not.Null);
                    if (obj.Object.Id == objectId)
                    {
                        found = true;
                    }
                }

                skipCount = skipCount + maxItems;

                if (children.HasMoreItems.HasValue)
                {
                    hasMore = children.HasMoreItems.Value;
                }
                else
                {
                    hasMore = children.Objects.Count == maxItems;
                }
            }

            Assert.That(found, Is.True);

            // check descendants
            if (RepositoryInfo.Capabilities == null ||
                RepositoryInfo.Capabilities.IsGetDescendantsSupported == null ||
                !(bool)RepositoryInfo.Capabilities.IsGetDescendantsSupported)
            {
                return;
            }

            found = false;
            IList<IObjectInFolderContainer> descendants = Binding.GetNavigationService().GetDescendants(RepositoryInfo.Id, folderId, 1, null, null, null, null, null, null);

            Assert.That(descendants, Is.Not.Null);

            foreach (IObjectInFolderContainer obj in descendants)
            {
                Assert.That(obj, Is.Not.Null);
                Assert.That(obj.Object, Is.Not.Null);
                Assert.That(obj.Object.Object, Is.Not.Null);
                Assert.That(obj.Object.Object.Id, Is.Not.Null);
                if (obj.Object.Object.Id == objectId)
                {
                    found = true;
                }
            }
            Assert.That(found, Is.True);
        }

        public void DeleteObject(string objectId)
        {
            Binding.GetObjectService().DeleteObject(RepositoryInfo.Id, objectId, true, null);

            try
            {
                Binding.GetObjectService().GetObject(RepositoryInfo.Id, objectId, null, null, null, null, null, null, null);
                Assert.Fail("CmisObjectNotFoundException excepted!");
            }
            catch (CmisObjectNotFoundException)
            {
            }
        }

        public string GetTextContent(string objectId)
        {
            IContentStream contentStream = Binding.GetObjectService().GetContentStream(RepositoryInfo.Id, objectId, null, null, null, null);

            Assert.That(contentStream, Is.Not.Null);
            Assert.That(contentStream.Stream, Is.Not.Null);

            MemoryStream memStream = new MemoryStream();
            byte[] buffer = new byte[4096];
            int b;
            while ((b = contentStream.Stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                memStream.Write(buffer, 0, b);
            }

            string result = Encoding.UTF8.GetString(memStream.GetBuffer(), 0, (int)memStream.Length);

            return result;
        }

        // ---- asserts ----

        public void AssertAreEqual(IObjectType expected, IObjectType actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            Assert.That(expected, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.Id, Is.Not.Null);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.IsBaseType, Is.EqualTo(expected.IsBaseType));
            Assert.That(actual.BaseTypeId, Is.EqualTo(expected.BaseTypeId));
            Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.LocalName, Is.EqualTo(expected.LocalName));
            Assert.That(actual.LocalNamespace, Is.EqualTo(expected.LocalNamespace));
            Assert.That(actual.QueryName, Is.EqualTo(expected.QueryName));
            Assert.That(actual.PropertyDefinitions.Count, Is.EqualTo(expected.PropertyDefinitions.Count));

            foreach (IPropertyDefinition propDef in expected.PropertyDefinitions)
            {
                Assert.That(propDef, Is.Not.Null);
                Assert.That(propDef.Id, Is.Not.Null);

                IPropertyDefinition propDef2 = actual[propDef.Id];

                AssertAreEqual(propDef, propDef2);
            }
        }

        public void AssertAreEqual(IPropertyDefinition expected, IPropertyDefinition actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            Assert.That(expected, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.Id, Is.Not.Null);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.LocalName, Is.EqualTo(expected.LocalName));
            Assert.That(actual.LocalNamespace, Is.EqualTo(expected.LocalNamespace));
            Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.QueryName, Is.EqualTo(expected.QueryName));
            Assert.That(actual.PropertyType, Is.EqualTo(expected.PropertyType));
            Assert.That(actual.Cardinality, Is.EqualTo(expected.Cardinality));
            Assert.That(actual.Updatability, Is.EqualTo(expected.Updatability));
        }
    }
}

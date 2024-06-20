using System;
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
using System.IO;
using System.Text;
using System.Linq;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data;
using DotCMIS.Data.Impl;
using DotCMIS.Enums;
using DotCMIS.Exceptions;
using NUnit.Framework;

namespace DotCMISUnitTest
{
    [TestFixture]
    class SmokeTest : TestFramework
    {
        [Test]
        public void SmokeTestSession()
        {
            Assert.That(Session, Is.Not.Null);
            Assert.That(Session.Binding, Is.Not.Null);
            Assert.That(Session.RepositoryInfo, Is.Not.Null);
            Assert.That(Session.RepositoryInfo.Id, Is.Not.Null);
            Assert.That(Session.RepositoryInfo.RootFolderId, Is.Not.Null);
            Assert.That(Session.DefaultContext, Is.Not.Null);
            Assert.That(Session.ObjectFactory, Is.Not.Null);

            Assert.That(Session.CreateObjectId("test").Id, Is.EqualTo("test"));
        }

        [Test]
        public void SmokeTestTypes()
        {
            // getTypeDefinition
            IObjectType documentType = Session.GetTypeDefinition("cmis:document");
            Assert.That(documentType, Is.Not.Null);
            Assert.That(documentType is DocumentType, Is.True);
            Assert.That(documentType.Id, Is.EqualTo("cmis:document"));
            Assert.That(documentType.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));
            Assert.That(documentType.IsBaseType, Is.True);
            Assert.That(documentType.ParentTypeId, Is.Null);
            Assert.That(documentType.PropertyDefinitions, Is.Not.Null);
            Assert.That(documentType.PropertyDefinitions.Count >= 9, Is.True);

            IObjectType folderType = Session.GetTypeDefinition("cmis:folder");
            Assert.That(folderType, Is.Not.Null);
            Assert.That(folderType is FolderType, Is.True);
            Assert.That(folderType.Id, Is.EqualTo("cmis:folder"));
            Assert.That(folderType.BaseTypeId, Is.EqualTo(BaseTypeId.CmisFolder));
            Assert.That(folderType.IsBaseType, Is.True);
            Assert.That(folderType.ParentTypeId, Is.Null);
            Assert.That(folderType.PropertyDefinitions, Is.Not.Null);
            Assert.That(folderType.PropertyDefinitions.Count >= 9, Is.True);

            // getTypeChildren
            Session.Clear();

            IItemEnumerable<IObjectType> children = Session.GetTypeChildren(null, true);
            Assert.That(children, Is.Not.Null);

            int count;
            count = 0;
            foreach (IObjectType type in children)
            {
                Assert.That(type, Is.Not.Null);
                Assert.That(type.Id, Is.Not.Null);
                Assert.That(type.IsBaseType, Is.True);
                Assert.That(type.ParentTypeId, Is.Null);
                Assert.That(type.PropertyDefinitions, Is.Not.Null);

                Session.Clear();
                IObjectType type2 = Session.GetTypeDefinition(type.Id);
                AssertAreEqual(type, type2);

                Session.GetTypeChildren(type.Id, true);

                count++;
            }

            Assert.That(count >= 2, Is.True);
            Assert.That(count <= 5, Is.True);

            // getTypeDescendants
            Session.Clear();

            IList<ITree<IObjectType>> descendants = Session.GetTypeDescendants(null, -1, true);

            count = 0;
            foreach (ITree<IObjectType> tree in descendants)
            {
                Assert.That(tree, Is.Not.Null);
                Assert.That(tree.Item, Is.Not.Null);

                IObjectType type = tree.Item;
                Assert.That(type, Is.Not.Null);
                Assert.That(type.Id, Is.Not.Null);
                Assert.That(type.IsBaseType, Is.True);
                Assert.That(type.ParentTypeId, Is.Null);
                Assert.That(type.PropertyDefinitions, Is.Not.Null);

                Session.Clear();
                IObjectType type2 = Session.GetTypeDefinition(type.Id);
                AssertAreEqual(type, type2);

                Session.GetTypeDescendants(type.Id, 2, true);

                count++;
            }

            Assert.That(count >= 2, Is.True);
            Assert.That(count <= 5, Is.True);
        }

        [Test]
        public void SmokeTestRootFolder()
        {
            ICmisObject rootFolderObject = Session.GetRootFolder();

            Assert.That(rootFolderObject, Is.Not.Null);
            Assert.That(rootFolderObject.Id, Is.Not.Null);
            Assert.That(rootFolderObject is IFolder, Is.True);

            IFolder rootFolder = (IFolder)rootFolderObject;

            Assert.That(rootFolder.Path, Is.EqualTo("/"));
            Assert.That(rootFolder.Paths.Count, Is.EqualTo(1));

            Assert.That(rootFolder.AllowableActions, Is.Not.Null);
            Assert.That(rootFolder.AllowableActions.Actions.Contains(Actions.CanGetProperties), Is.True);
            Assert.That(rootFolder.AllowableActions.Actions.Contains(Actions.CanGetFolderParent), Is.False);

            IItemEnumerable<ICmisObject> children = rootFolder.GetChildren();
            Assert.That(children, Is.Not.Null);
            foreach (ICmisObject child in children)
            {
                Assert.That(child, Is.Not.Null);
                Assert.That(child.Id, Is.Not.Null);
                Assert.That(child.Name, Is.Not.Null);
                Console.WriteLine(child.Name + " (" + child.Id + ")");
            }
        }

        [Test]
        public void SmokeTestQuery()
        {
            IItemEnumerable<IQueryResult> qr = Session.Query("SELECT * FROM cmis:document", false);
            Assert.That(qr, Is.Not.Null);

            foreach (IQueryResult hit in qr)
            {
                Assert.That(hit, Is.Not.Null);
                Assert.That(hit["cmis:objectId"], Is.Not.Null);
                Console.WriteLine(hit.GetPropertyValueById(PropertyIds.Name) + " (" + hit.GetPropertyValueById(PropertyIds.ObjectId) + ")");

                foreach (IPropertyData prop in hit.Properties)
                {
                    string name = prop.QueryName;
                    object value = prop.FirstValue;
                }
            }
        }

        [Test]
        public void SmokeTestCreateDocument()
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test-smoke.txt";
            properties[PropertyIds.ObjectTypeId] = DefaultDocumentType;

            byte[] content = UTF8Encoding.UTF8.GetBytes("Hello World!");

            ContentStream contentStream = new ContentStream();
            contentStream.FileName = properties[PropertyIds.Name] as string;
            contentStream.MimeType = "text/plain";
            contentStream.Length = content.Length;
            contentStream.Stream = new MemoryStream(content);

            IDocument doc = TestFolder.CreateDocument(properties, contentStream, null);

            // check doc
            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Id, Is.Not.Null);
            Assert.That(doc.Name, Is.EqualTo(properties[PropertyIds.Name]));
            Assert.That(doc.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));
            Assert.That(doc.AllowableActions.Actions.Contains(Actions.CanGetProperties), Is.True);
            Assert.That(doc.AllowableActions.Actions.Contains(Actions.CanGetChildren), Is.False);

            // check type
            IObjectType type = doc.ObjectType;
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Id, Is.Not.Null);
            Assert.That(type.Id, Is.EqualTo(properties[PropertyIds.ObjectTypeId]));

            // check versions
            IList<IDocument> versions = doc.GetAllVersions();
            Assert.That(versions, Is.Not.Null);
            Assert.That(versions.Count, Is.EqualTo(1));
            //Assert.AreEqual(doc.Id, versions[0].Id);

            // check content
            IContentStream retrievedContentStream = doc.GetContentStream();
            Assert.That(retrievedContentStream, Is.Not.Null);
            Assert.That(retrievedContentStream.Stream, Is.Not.Null);

            MemoryStream byteStream = new MemoryStream();
            byte[] buffer = new byte[4096];
            int b = 1;
            while (b > 0)
            {
                b = retrievedContentStream.Stream.Read(buffer, 0, buffer.Length);
                byteStream.Write(buffer, 0, b);
            }

            byte[] retrievedContent = byteStream.ToArray();
            Assert.That(retrievedContent, Is.Not.Null);
            Assert.That(retrievedContent.Length, Is.EqualTo(content.Length));
            for (int i = 0; i < content.Length; i++)
            {
                Assert.That(retrievedContent[i], Is.EqualTo(content[i]));
            }

            // update name
            properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test2-smoke.txt";

            IObjectId newId = doc.UpdateProperties(properties);
            IDocument doc2 = Session.GetObject(newId) as IDocument;

            Assert.That(doc2, Is.Not.Null);

            doc2.Refresh();
            Assert.That(doc2.Name, Is.EqualTo(properties[PropertyIds.Name]));
            Assert.That(doc2.GetPropertyValue(PropertyIds.Name), Is.EqualTo(properties[PropertyIds.Name]));

            IProperty nameProperty = doc2[PropertyIds.Name];
            Assert.That(nameProperty.PropertyType, Is.Not.Null);
            Assert.That(nameProperty.Value, Is.EqualTo(properties[PropertyIds.Name]));
            Assert.That(nameProperty.FirstValue, Is.EqualTo(properties[PropertyIds.Name]));


            byte[] content2 = UTF8Encoding.UTF8.GetBytes("Hello Universe!");

            ContentStream contentStream2 = new ContentStream();
            contentStream2.FileName = properties[PropertyIds.Name] as string;
            contentStream2.MimeType = "text/plain";
            contentStream2.Length = content2.Length;
            contentStream2.Stream = new MemoryStream(content2);

            // doc.SetContentStream(contentStream2, true);

            doc2.Delete(true);

            try
            {
                doc.Refresh();
                Assert.Fail("Document shouldn't exist anymore!");
            }
            catch (CmisObjectNotFoundException) { }
        }

        [Test]
        public void SmokeTestVersioning()
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test-version-smoke.txt";
            properties[PropertyIds.ObjectTypeId] = DefaultDocumentType;

            IDocument doc = TestFolder.CreateDocument(properties, null, VersioningState.Major);
            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Id, Is.Not.Null);
            Assert.That(doc.Name, Is.EqualTo(properties[PropertyIds.Name]));

            IList<IDocument> versions = doc.GetAllVersions();
            Assert.That(versions, Is.Not.Null);
            Assert.That(versions.Count, Is.EqualTo(1));

            IObjectId pwcId = doc.CheckOut();
            Assert.That(pwcId, Is.Not.Null);

            IDocument pwc = Session.GetObject(pwcId) as IDocument;

            // check PWC
            Assert.That(pwc, Is.Not.Null);
            Assert.That(pwc.Id, Is.Not.Null);
            Assert.That(doc.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));

            IDictionary<string, object> newProperties = new Dictionary<string, object>();
            newProperties[PropertyIds.Name] = "test-version2-smoke.txt";

            IObjectId doc2Id = pwc.CheckIn(true, newProperties, null, "new DotCMIS version");
            Assert.That(doc2Id, Is.Not.Null);

            IDocument doc2 = Session.GetObject(doc2Id) as IDocument;
            doc2.Refresh();

            // check new version
            Assert.That(doc2, Is.Not.Null);
            Assert.That(doc2.Id, Is.Not.Null);
            // Assert.AreEqual(newProperties[PropertyIds.Name], doc2.Name);
            Assert.That(doc2.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));

            versions = doc2.GetAllVersions();
            Assert.That(versions, Is.Not.Null);
            Assert.That(versions.Count, Is.EqualTo(2));

            IDocument last1 = doc.GetObjectOfLatestVersion(false);
            Assert.That(last1.Id, Is.EqualTo(doc2.Id));

            IOperationContext oc = Session.CreateOperationContext();
            oc.CacheEnabled = false;
            IDocument last2 = Session.GetLatestDocumentVersion(doc.Id, oc);
            Assert.That(last2.Id, Is.EqualTo(doc2.Id));

            doc2.DeleteAllVersions();

            try
            {
                doc2.Refresh();
                Assert.Fail("Document shouldn't exist anymore!");
            }
            catch (CmisObjectNotFoundException) { }
        }

        [Test]
        public void SmokeTestCreateFolder()
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test-smoke";
            properties[PropertyIds.ObjectTypeId] = DefaultFolderType;

            IFolder folder = TestFolder.CreateFolder(properties);

            // check folder
            Assert.That(folder, Is.Not.Null);
            Assert.That(folder.Id, Is.Not.Null);
            Assert.That(folder.Name, Is.EqualTo(properties[PropertyIds.Name]));
            Assert.That(folder.BaseTypeId, Is.EqualTo(BaseTypeId.CmisFolder));
            Assert.That(folder.FolderParent.Id, Is.EqualTo(TestFolder.Id));
            Assert.That(folder.IsRootFolder, Is.False);
            Assert.That(folder.Path.StartsWith("/"), Is.True);
            Assert.That(folder.AllowableActions.Actions.Contains(Actions.CanGetProperties), Is.True);
            Assert.That(folder.AllowableActions.Actions.Contains(Actions.CanGetChildren), Is.True);
            Assert.That(folder.AllowableActions.Actions.Contains(Actions.CanGetContentStream), Is.False);

            // rename folder
            folder.Rename("test-smoke-renamed");
            Assert.That(folder.Name, Is.EqualTo("test-smoke-renamed"));

            // check children
            foreach (ICmisObject cmisObject in folder.GetChildren())
            {
                Assert.Fail("Folder shouldn't have children!");
            }

            // check descendants
            bool? descSupport = Session.RepositoryInfo.Capabilities.IsGetDescendantsSupported;
            if (descSupport.HasValue && descSupport.Value)
            {
                IList<ITree<IFileableCmisObject>> list = folder.GetDescendants(-1);

                if (list != null)
                {
                    foreach (ITree<IFileableCmisObject> desc in list)
                    {
                        Assert.Fail("Folder shouldn't have children!");
                    }
                }
            }
            else
            {
                Console.WriteLine("GetDescendants not supported!");
            }

            // check folder tree
            bool? folderTreeSupport = Session.RepositoryInfo.Capabilities.IsGetFolderTreeSupported;
            if (folderTreeSupport.HasValue && folderTreeSupport.Value)
            {
                IList<ITree<IFileableCmisObject>> list = folder.GetFolderTree(-1);

                if (list != null)
                {
                    foreach (ITree<IFileableCmisObject> desc in list)
                    {
                        Assert.Fail("Folder shouldn't have children!");
                    }
                }
            }
            else
            {
                Console.WriteLine("GetFolderTree not supported!");
            }

            // check parents
            IFolder parent = folder.FolderParent;
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Id, Is.EqualTo(TestFolder.Id));

            IList<IFolder> parents = folder.Parents;
            Assert.That(parents, Is.Not.Null);
            Assert.That(parents.Count > 0, Is.True);

            bool found = false;
            foreach (IFolder p in parents)
            {
                if (TestFolder.Id == p.Id)
                {
                    found = true;
                    break;
                }
            }
            Assert.That(found, Is.True);

            folder.Delete(true);

            try
            {
                folder.Refresh();
                Assert.Fail("Folder shouldn't exist anymore!");
            }
            catch (CmisObjectNotFoundException) { }
        }

        [Test]
        public void SmokeTestContentChanges()
        {
            if (Session.RepositoryInfo.Capabilities.ChangesCapability != null)
            {
                if (Session.RepositoryInfo.Capabilities.ChangesCapability != CapabilityChanges.None)
                {
                    IChangeEvents changeEvents = Session.GetContentChanges(null, true, 1000);
                    Assert.That(changeEvents, Is.Not.Null);
                }
                else
                {
                    Console.WriteLine("Content changes not supported!");
                }
            }
            else
            {
                Console.WriteLine("ChangesCapability not set!");
            }
        }

        [Test]
        public void SmokeTestCMIS609()
        {
            IFolder rootFolder = Session.GetRootFolder();
            IEnumerable<ICmisObject> children = rootFolder.GetChildren();
            List<ICmisObject> childrenList = children.ToList();
        }
    }
}

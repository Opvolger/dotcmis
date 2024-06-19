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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using DotCMIS.Data;
using DotCMIS.Enums;
using DotCMIS.Exceptions;

namespace DotCMISUnitTest
{
    [TestFixture]
    public class TypeTest : TestFramework
    {
        [Test]
        public void TestBaseTypes()
        {
            ITypeDefinition type;

            // cmis:document
            type = Binding.GetRepositoryService().GetTypeDefinition(RepositoryInfo.Id, "cmis:document", null);

            Assert.That(type, Is.Not.Null);
            Assert.That(type.BaseTypeId, Is.EqualTo(BaseTypeId.CmisDocument));
            Assert.That(type.Id, Is.EqualTo("cmis:document"));

            // cmis:folder
            type = Binding.GetRepositoryService().GetTypeDefinition(RepositoryInfo.Id, "cmis:folder", null);

            Assert.That(type, Is.Not.Null);
            Assert.That(type.BaseTypeId, Is.EqualTo(BaseTypeId.CmisFolder));
            Assert.That(type.Id, Is.EqualTo("cmis:folder"));

            // cmis:relationship
            try
            {
                type = Binding.GetRepositoryService().GetTypeDefinition(RepositoryInfo.Id, "cmis:relationship", null);

                Assert.That(type, Is.Not.Null);
                Assert.That(type.BaseTypeId, Is.EqualTo(BaseTypeId.CmisRelationship));
                Assert.That(type.Id, Is.EqualTo("cmis:relationship"));
            }
            catch (CmisObjectNotFoundException)
            {
                // not supported by the repository
            }

            // cmis:policy
            try
            {
                type = Binding.GetRepositoryService().GetTypeDefinition(RepositoryInfo.Id, "cmis:policy", null);

                Assert.That(type, Is.Not.Null);
                Assert.That(type.BaseTypeId, Is.EqualTo(BaseTypeId.CmisPolicy));
                Assert.That(type.Id, Is.EqualTo("cmis:policy"));
            }
            catch (CmisObjectNotFoundException)
            {
                // not supported by the repository
            }
        }

        [Test]
        public void TestTypeChildren()
        {
            ITypeDefinitionList typeList = Binding.GetRepositoryService().GetTypeChildren(RepositoryInfo.Id, null, null, null, null, null);

            Assert.That(typeList, Is.Not.Null);
            Assert.That(typeList.List, Is.Not.Null);
            Assert.That(typeList.NumItems, Is.Not.Null);
            Assert.That(typeList.NumItems >= 2, Is.True);
            Assert.That(typeList.NumItems <= 5, Is.True);

            bool foundDocument = false;
            bool foundFolder = false;
            foreach (ITypeDefinition type in typeList.List)
            {
                Assert.That(type, Is.Not.Null);
                Assert.That(type.Id, Is.Not.Null);

                if (type.Id == "cmis:document")
                {
                    foundDocument = true;
                }
                if (type.Id == "cmis:folder")
                {
                    foundFolder = true;
                }
            }

            Assert.That(foundDocument, Is.True);
            Assert.That(foundFolder, Is.True);
        }
    }
}

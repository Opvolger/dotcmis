﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using DotCMIS;
using DotCMIS.Client.Impl;
using DotCMIS.Client;

namespace DotCMISUnitTest
{
    [TestFixture]
    class GetChildrenTest
    {
        private static int numOfDocuments = 250;

        [Test]
        public void TestPaging()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters[SessionParameter.BindingType] = BindingType.AtomPub;
            parameters[SessionParameter.AtomPubUrl] = AppSettingsHelper.GetAppSetting(SessionParameter.AtomPubUrl);
            parameters[SessionParameter.User] = AppSettingsHelper.GetAppSetting(SessionParameter.User);
            parameters[SessionParameter.Password] = AppSettingsHelper.GetAppSetting(SessionParameter.Password);

            SessionFactory factory = SessionFactory.NewInstance();
            ISession session = factory.GetRepositories(parameters)[0].CreateSession();

            IOperationContext oc = session.CreateOperationContext();
            oc.MaxItemsPerPage = 100;

            IFolder folder = createData(session);
            //IFolder folder = session.GetObjectByPath(@"/childrenTestFolder") as IFolder;

            int counter = 0;
            foreach (ICmisObject child in folder.GetChildren(oc))
            {
                Console.WriteLine("!" + counter + " " + child.Name);
                counter++;
            }

            Assert.That(counter, Is.EqualTo(numOfDocuments));

            counter = 0;
            foreach (ICmisObject child in folder.GetChildren(oc).GetPage(150))
            {
                Console.WriteLine("#" + counter + " " + child.Name);
                counter++;
            }

            Assert.That(counter, Is.EqualTo(150));

            counter = 0;
            foreach (ICmisObject child in folder.GetChildren(oc).SkipTo(20).GetPage(180))
            {
                Console.WriteLine("*" + counter + " " + child.Name);
                counter++;
            }

            Assert.That(counter, Is.EqualTo(180));

            folder.DeleteTree(true, null, true);
        }


        private IFolder createData(ISession session)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "childrenTestFolder";
            properties[PropertyIds.ObjectTypeId] = "cmis:folder";

            IFolder folder = session.GetRootFolder().CreateFolder(properties);

            for (int i = 0; i < numOfDocuments; i++)
            {
                Dictionary<string, object> docProps = new Dictionary<string, object>();
                docProps[PropertyIds.Name] = "doc" + i;
                docProps[PropertyIds.ObjectTypeId] = "cmis:document";

                folder.CreateDocument(docProps, null, null);

            }

            return folder;
        }
    }
}

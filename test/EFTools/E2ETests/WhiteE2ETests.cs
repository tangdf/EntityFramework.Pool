﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using SystemThread = System.Threading.Thread;

namespace EFDesigner.E2ETests
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Windows.Automation;
    using System.Xml.Linq;
    using EFDesignerTestInfrastructure;
    using EFDesignerTestInfrastructure.VS;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestTools.VsIdeTesting;
    using TestStack.White;
    using TestStack.White.Configuration;
    using TestStack.White.Factory;
    using TestStack.White.InputDevices;
    using TestStack.White.UIItems;
    using TestStack.White.UIItems.Finders;
    using TestStack.White.UIItems.ListBoxItems;
    using TestStack.White.UIItems.TreeItems;
    using TestStack.White.UIItems.WPFUIItems;
    using Process = System.Diagnostics.Process;
    using Window = TestStack.White.UIItems.WindowItems.Window;

    /// <summary>
    ///     This class has End to end tests for designer UI
    /// </summary>
    [TestClass]
    public class WhiteE2ETests
    {
        private readonly ResourcesHelper _resourceHelper;
        private Application _visualStudio;
        private Window _visualStudioMainWindow;
        private Window _wizard;
        private int _nameSuffix = 1;

        // Define model db attributes
        private const string ModelName = "SchoolModel";
        private const string ProjectPrefix = "ExistingDBTest";
        private static int _projIndex;

        private static readonly List<string> _entityNames = new List<string>
        {
            "C__MigrationHistory",
            "Departments",
            "Courses",
            "People",
            "StudentGrades",
            "Courses"
        };

        private static readonly List<string> _entityAttributes = new List<string>
        {
            "PersonID",
            "LastName",
            "FirstName",
            "HireDate",
            "EnrollmentDate",
            "Discriminator"
        };

        private enum AssociationType
        {
            OneToOne,
            OneToMany,
            ManyToMany
        }

        public WhiteE2ETests()
        {
            _resourceHelper = new ResourcesHelper();
        }

        public TestContext TestContext { get; set; }

        private static DTE Dte
        {
            get { return VsIdeTestHostContext.Dte; }
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            // Create a simple DB for tests to use
            CreateDatabase();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            DropDatabase();
        }

        /// <summary>
        ///     Simple E2E test that selects empty model in the wizard
        /// </summary>
        [TestMethod]
        [HostType("VS IDE")]
        [TestCategory("DoNotRunOnCI")]
        public void AddAnEmptyModel()
        {
            Exception exceptionCaught = null;
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Starting the test");

            var project = Dte.CreateProject(
                TestContext.TestRunDirectory,
                "EmptyModelTest",
                DteExtensions.ProjectKind.Executable,
                DteExtensions.ProjectLanguage.CSharp);

            Assert.IsNotNull(project, "Could not create project");

            var wizardDiscoveryThread = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        _visualStudio = Application.Attach(Process.GetCurrentProcess().Id);
                        _visualStudioMainWindow = _visualStudio.GetWindow(
                            SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                            InitializeOption.NoCache);

                        var newItemWindow = _visualStudioMainWindow.Popup;
                        var extensions = newItemWindow.Get<ListView>(SearchCriteria.ByText("Extensions"));
                        extensions.Select("ADO.NET Entity Data Model");
                        var addButton = newItemWindow.Get<Button>(SearchCriteria.ByAutomationId("btn_OK"));
                        addButton.Click();

                        _wizard = GetWizard("WizardFormDialog_Title");

                        // Walk thru the Empty model selection
                        CreateEmptyModel();
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                    }
                }, "UIExecutor");

            Dte.ExecuteCommand("Project.AddNewItem");

            wizardDiscoveryThread.Join();

            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "wizardDiscoveryThread returned");

            // Make sure the expected files are created
            var csfile = project.GetProjectItemByName("Model1.designer.cs");
            Assert.IsNotNull(csfile, "csfile is not null");

            var diagramFile = project.GetProjectItemByName("Model1.edmx.diagram");
            Assert.IsNotNull(diagramFile, "diagramfile not null");

            var errors = Build();
            Assert.IsTrue(errors == null || errors.Count == 0);

            var entities = AddEntities();
            AddAssociations(entities);

            var designerSurface = _visualStudioMainWindow.Get<Panel>(SearchCriteria.ByText("Modeling Design Surface"));
            AddInheritance(designerSurface, entities[1], entities[3]);
            AddEnum(designerSurface);

            project.Save();
            CheckFilesEmptyModel(project);
        }

        [TestMethod]
        [HostType("VS IDE")]
        [Timeout(4 * 60 * 1000)]
        [TestCategory("DoNotRunOnCI")]
        public void AddModelFromExistingDB()
        {
            Exception exceptionCaught = null;
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Starting the test");

            // On the main thread create a project
            var project = Dte.CreateProject(
                TestContext.TestRunDirectory,
                GetRandomProjectName(),
                DteExtensions.ProjectKind.Executable,
                DteExtensions.ProjectLanguage.CSharp);

            Assert.IsNotNull(project, "Could not create project");

            // We need to create this thread to keep polling for wizard to show up and
            // walk thru the wizard. DTE call just launches the wizard and stays there
            // taking up the main thread
            var wizardDiscoveryThread = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In thread wizardDiscoveryThread");

                        _visualStudio = Application.Attach(Process.GetCurrentProcess().Id);
                        _visualStudioMainWindow = _visualStudio.GetWindow(
                            SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                            InitializeOption.NoCache);

                        var newItemWindow = _visualStudioMainWindow.Popup;
                        var extensions = newItemWindow.Get<ListView>(SearchCriteria.ByText("Extensions"));
                        extensions.Select("ADO.NET Entity Data Model");
                        var addButton = newItemWindow.Get<Button>(SearchCriteria.ByAutomationId("btn_OK"));
                        addButton.Click();

                        // This method polls for the the wizard to show up
                        _wizard = GetWizard("WizardFormDialog_Title");

                        // Walk thru the Wizard with existing DB option
                        CreateModelFromDB("School", codeFirst: false);
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Wizard Discovery thread exception:" + ex);
                    }
                }, "UIExecutor");

            Dte.ExecuteCommand("Project.AddNewItem");

            wizardDiscoveryThread.Join();

            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            // Make sure all expected files are generated.
            // Make sure EDMX is what you expected.
            CheckFilesExistingModel(project);

            // Build the project
            var errors = Build();
            Assert.IsTrue(errors == null || errors.Count == 0);

            // Check Model browser, mapping window, properties window to spot check them
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var modelBrowserTree = _visualStudioMainWindow.Get<Tree>(SearchCriteria.ByAutomationId("ExplorerTreeView"));

            // See if entities exist in diagram
            var diagramNode = modelBrowserTree.Node("Model1.edmx", "Diagrams Diagrams", "Diagram Diagram1");
            ((ExpandCollapsePattern)diagramNode.AutomationElement.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            foreach (var childNode in diagramNode.Nodes)
            {
                Dte.ExecuteCommand("View.EntityDataModelBrowser");
                ((SelectionItemPattern)childNode.AutomationElement.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();

                var currentNode = _entityNames.Find(el => el.Equals(childNode.Text));

                if (string.IsNullOrEmpty(currentNode)
                    || currentNode.Contains("Migration"))
                {
                    continue;
                }
                CheckProperties(ModelName + "." + currentNode + ":EntityType");
                Assert.AreEqual("EntityTypeShape " + currentNode, childNode.Text);
            }

            // See if entities exist in model and have expected properties
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var modelNode = modelBrowserTree.Node(
                "Model1.edmx", "ConceptualEntityModel " + ModelName,
                "Entity Types");
            CheckEntityProperties(modelNode, "ConceptualEntityType {0}", "ConceptualProperty ", "{0}.{1}.{2}:Property");

            // See if entities exist in store and have expected properties
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var storeNode = modelBrowserTree.Node(
                "Model1.edmx", "StorageEntityModel " + ModelName + ".Store",
                "Tables / Views");
            CheckEntityProperties(storeNode, "StorageEntityType {0}s", "StorageProperty ", "{0}.Store.{1}s.{2}:Property");

            // Future: Automate the designer surface.

            // Make sure app.config is updated correctly
            CheckAppConfig(project);

            Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":Close Solution");
            Dte.CloseSolution(false);
        }

        [TestMethod]
        [HostType("VS IDE")]
        [Timeout(4 * 60 * 1000)]
        [TestCategory("DoNotRunOnCI")]
        public void AddModelFromExistingDBChangeDefaults()
        {
            Exception exceptionCaught = null;
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Starting the test");

            var project = Dte.CreateProject(
                TestContext.TestRunDirectory,
                "ExistingDBTestChangeDefaults",
                DteExtensions.ProjectKind.Executable,
                DteExtensions.ProjectLanguage.CSharp);

            Assert.IsNotNull(project, "Could not create project");

            // We need to create this thread to keep polling for wizard to show up and
            // walk thru the wizard. DTE call just launches the wizard and stays there
            // taking up the main thread
            var wizardDiscoveryThread = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In thread wizardDiscoveryThread");

                        _visualStudio = Application.Attach(Process.GetCurrentProcess().Id);
                        _visualStudioMainWindow = _visualStudio.GetWindow(
                            SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                            InitializeOption.NoCache);

                        var newItemWindow = _visualStudioMainWindow.Popup;
                        var extensions = newItemWindow.Get<ListView>(SearchCriteria.ByText("Extensions"));
                        extensions.Select("ADO.NET Entity Data Model");
                        var addButton = newItemWindow.Get<Button>(SearchCriteria.ByAutomationId("btn_OK"));
                        addButton.Click();

                        // This method polls for the the wizard to show up
                        _wizard = GetWizard("WizardFormDialog_Title");

                        // Walk thru the Wizard with existing DB option
                        CreateModelFromDBNonDefaults("School");
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                    }
                }, "UIExecutor");

            Dte.ExecuteCommand("Project.AddNewItem");

            wizardDiscoveryThread.Join();

            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            // Build the project
            var errors = Build();
            Assert.IsTrue(errors == null || errors.Count == 0);
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestCategory("DoNotRunOnCI")]
        public void AddModelFromDBUpdateModel()
        {
            Exception exceptionCaught = null;
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Starting the test");

            // We need to create this thread to keep polling for wizard to show up and
            // walk thru the wizard. DTE call just launches the wizard and stays there
            // taking up the main thread
            var wizardDiscoveryThread = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In thread wizardDiscoveryThread");

                        // This method polls for the the wizard to show up
                        _wizard = GetWizard("WizardFormDialog_Title");

                        // Walk thru the Wizard with existing DB option
                        CreateModelFromDB("School", codeFirst: false);
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                    }
                }, "UIExecutor");

            // On the main thread create a project
            var project = Dte.CreateProject(
                TestContext.TestRunDirectory,
                "UpdateFromDBTest",
                DteExtensions.ProjectKind.Executable,
                DteExtensions.ProjectLanguage.CSharp);

            Assert.IsNotNull(project, "Could not create project");

            // Create model from a small size database
            DteExtensions.AddNewItem(Dte, @"Data\ADO.NET Entity Data Model", "Model1", project);

            wizardDiscoveryThread.Join();
            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            // Update the sample db
            const string createTable = "CREATE TABLE Books (BookID INTEGER PRIMARY KEY IDENTITY, " +
                                       "Title CHAR(50) NOT NULL, Author CHAR(50), " +
                                       "PageCount INTEGER, Topic CHAR(30), Code CHAR(15))";

#if (VS14 || VS15)
            const string connectionString = @"Data Source=(localdb)\mssqllocaldb;initial catalog=School;integrated security=True;Pooling=false";
#else
            const string connectionString = @"Data Source=(localdb)\v11.0;initial catalog=School;integrated security=True;Pooling=false";
#endif
            ExecuteSqlCommand(connectionString, createTable);

            // Update model from db
            var wizardDiscoveryThread2 = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In thread wizardDiscoveryThread");

                        // This method polls for the the wizard to show up
                        _wizard = GetWizard("UpdateFromDatabaseWizard_Title");

                        // Walk thru the Wizard with existing DB option
                        UpdateModelFromDBWizard();
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                    }
                }, "UIExecutor");

            var designerSurface = _visualStudioMainWindow.Get<Panel>(SearchCriteria.ByText("Modeling Design Surface"));
            designerSurface.RightClickAt(designerSurface.Location);
            var popUpMenu = _visualStudioMainWindow.Popup;
            var menuItem = popUpMenu.Item("Update Model from Database...");
            menuItem.Click();

            wizardDiscoveryThread2.Join();
            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            // Make sure all expected files are generated.
            // Make sure EDMX is what you expected.
            CheckFilesExistingModel(project);

            // Build the project
            var errors = Build();
            Assert.IsTrue(errors == null || errors.Count == 0);

            // Check Model browser, mapping window, properties window to spot check them
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var modelBrowserTree = _visualStudioMainWindow.Get<Tree>(SearchCriteria.ByAutomationId("ExplorerTreeView"));

            // See if entities exist in diagram
            var diagramNode = modelBrowserTree.Node("Model1.edmx", "Diagrams Diagrams", "Diagram Diagram1");
            ((ExpandCollapsePattern)diagramNode.AutomationElement.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();

            var updatedEntityNames = _entityNames;
            updatedEntityNames.Add("Books");
            foreach (var childNode in diagramNode.Nodes)
            {
                Dte.ExecuteCommand("View.EntityDataModelBrowser");
                ((SelectionItemPattern)childNode.AutomationElement.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();

                var currentNode = updatedEntityNames.Find(el => el.Equals(childNode.Text));

                if (string.IsNullOrEmpty(currentNode)
                    || currentNode.Contains("Migration"))
                {
                    continue;
                }
                CheckProperties(ModelName + "." + currentNode + ":EntityType");
                Assert.AreEqual("EntityTypeShape " + currentNode, childNode.Text);
            }

            // See if entities exist in model and have expected properties
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var modelNode = modelBrowserTree.Node(
                "Model1.edmx", "ConceptualEntityModel " + ModelName,
                "Entity Types");
            CheckEntityProperties(modelNode, "ConceptualEntityType {0}", "ConceptualProperty ", "{0}.{1}.{2}:Property");

            // See if entities exist in store and have expected properties
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            var storeNode = modelBrowserTree.Node(
                "Model1.edmx", "StorageEntityModel " + ModelName + ".Store",
                "Tables / Views");
            CheckEntityProperties(storeNode, "StorageEntityType {0}s", "StorageProperty ", "{0}.Store.{1}s.{2}:Property");
        }

        [TestMethod]
        [HostType("VS IDE")]
        [TestCategory("DoNotRunOnCI")]
        public void CodeFirstAddModelFromDB()
        {
            Exception exceptionCaught = null;
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + "Starting the test");

            // We need to create this thread to keep polling for wizard to show up and
            // walk thru the wizard. DTE call just launches the wizard and stays there
            // taking up the main thread
            var wizardDiscoveryThread = ExecuteThreadedAction(
                () =>
                {
                    try
                    {
                        Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In thread wizardDiscoveryThread");

                        // This method polls for the the wizard to show up
                        _wizard = GetWizard("WizardFormDialog_Title");

                        // Walk thru the Wizard with existing DB option, code first
                        CreateModelFromDB("School", codeFirst: true);
                    }
                    catch (Exception ex)
                    {
                        exceptionCaught = ex;
                    }
                }, "UIExecutor");

            // On the main thread create a project
            var project = Dte.CreateProject(
                TestContext.TestRunDirectory,
                "CodeFirstAddModelFromDBTest",
                DteExtensions.ProjectKind.Executable,
                DteExtensions.ProjectLanguage.CSharp);

            Assert.IsNotNull(project, "Could not create project");

            // Create model from a small size database
            DteExtensions.AddNewItem(Dte, @"Data\ADO.NET Entity Data Model", "Model1", project);

            wizardDiscoveryThread.Join();
            if (exceptionCaught != null)
            {
                throw exceptionCaught;
            }

            // Make sure all expected files are generated.
            CheckFilesCodeFirst(project);
        }

        private void CheckProperties(string property)
        {
            Dte.ExecuteCommand("View.PropertiesWindow", string.Empty);
            var componentsBox = _visualStudioMainWindow.Get<ComboBox>(SearchCriteria.ByText("Components"));
            Assert.AreEqual(property, componentsBox.Item(0).Text);
        }

        private void CheckEntityProperties(TreeNode node, string entityType, string entityProperty, string propertyValue)
        {
            Dte.ExecuteCommand("View.EntityDataModelBrowser");
            ((ExpandCollapsePattern)node.AutomationElement.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            foreach (var childNode in node.Nodes)
            {
                var currentNode = _entityNames.Find(el => el.Equals(childNode.Text));
                if (string.IsNullOrEmpty(currentNode))
                {
                    continue;
                }
                Assert.AreEqual(string.Format(entityType, currentNode), childNode.Text);
                Dte.ExecuteCommand("View.EntityDataModelBrowser");
                ((ExpandCollapsePattern)childNode.AutomationElement.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
                foreach (var propertyNode in childNode.Nodes)
                {
                    Dte.ExecuteCommand("View.EntityDataModelBrowser");
                    ((SelectionItemPattern)propertyNode.AutomationElement.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
                    var cleanProperty = propertyNode.Text.Replace(entityProperty, "");

                    CheckProperties(string.Format(propertyValue, ModelName, currentNode, cleanProperty));
                    Assert.IsTrue(_entityAttributes.Contains(cleanProperty));
                }
            }
        }

        private static void CheckAppConfig(Project project)
        {
            var appConfigFile = project.GetProjectItemByName("app.config");
            var appConfigPath = appConfigFile.Properties.Item("FullPath").Value.ToString();

            var xmlEntries = XDocument.Load(appConfigPath);

            var element = xmlEntries.Elements("configuration")
                .Elements("connectionStrings")
                .Elements("add").First();
            Assert.AreEqual((string)element.Attribute("name"), "SchoolEntities");
        }

        private static void CheckFilesEmptyModel(Project project)
        {
            var edmxFile = project.GetProjectItemByName("Model1.edmx");
            Assert.IsNotNull(edmxFile, "edmxfile is null");

            var edmxPath = edmxFile.Properties.Item("FullPath").Value.ToString();
            var xmlEntries = XDocument.Load(edmxPath);
            var edmx = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edmx");
            var edm = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm");

            var conceptualElement = xmlEntries.Elements(edmx + "Edmx")
                .Elements(edmx + "Runtime")
                .Elements(edmx + "ConceptualModels")
                .Elements(edm + "Schema").First();
            Assert.AreEqual((string)conceptualElement.Attribute("Namespace"), "Model1");

            var entity1Element = (from el in conceptualElement.Descendants(edm + "EntityType")
                where (string)el.Attribute("Name") == "Entity_1"
                select el).First();
            Assert.IsNotNull(entity1Element);

            var entity2Element = (from el in conceptualElement.Descendants(edm + "EntityType")
                where (string)el.Attribute("Name") == "Entity_2"
                select el).First();
            Assert.IsNotNull(entity2Element);

            var entity3Element = (from el in conceptualElement.Descendants(edm + "EntityType")
                where (string)el.Attribute("Name") == "Entity_3"
                select el).First();
            Assert.IsNotNull(entity3Element);

            var entity4Element = (from el in conceptualElement.Descendants(edm + "EntityType")
                where (string)el.Attribute("Name") == "Entity_4"
                select el).First();
            Assert.IsNotNull(entity4Element);

            var association1Element = (from el in conceptualElement.Descendants(edm + "Association")
                where (string)el.Attribute("Name") == "Entity_1Entity_2"
                select el).First();
            Assert.IsNotNull(association1Element);

            var association2Element = (from el in conceptualElement.Descendants(edm + "Association")
                where (string)el.Attribute("Name") == "Entity_3Entity_4"
                select el).First();
            Assert.IsNotNull(association2Element);

            var association3Element = (from el in conceptualElement.Descendants(edm + "Association")
                where (string)el.Attribute("Name") == "Entity_1Entity_3"
                select el).First();
            Assert.IsNotNull(association3Element);

            var enumElement = (from el in conceptualElement.Descendants(edm + "EnumType")
                where (string)el.Attribute("Name") == "EnumType5"
                select el).First();
            Assert.IsNotNull(enumElement);
        }

        private static void CheckFilesExistingModel(Project project)
        {
            var csfile = project.GetProjectItemByName("Model1.designer.cs");
            Assert.IsNotNull(csfile, "csfile is null");

            var diagramFile = project.GetProjectItemByName("Model1.edmx.diagram");
            Assert.IsNotNull(diagramFile, "diagramfile is null");

            var contextFile = project.GetProjectItemByName("Model1.context.cs");
            Assert.IsNotNull(contextFile, "context is null");

            var ttFile = project.GetProjectItemByName("Model1.tt");
            Assert.IsNotNull(ttFile, "ttfile is null");

            var personfile = project.GetProjectItemByName("person.cs");
            Assert.IsNotNull(personfile, "personfile is null");

            var edmxFile = project.GetProjectItemByName("Model1.edmx");
            Assert.IsNotNull(edmxFile, "edmxfile is null");
            var edmxPath = edmxFile.Properties.Item("FullPath").Value.ToString();
            var xmlEntries = XDocument.Load(edmxPath);
            var edmx = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edmx");
            var ssdl = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm/ssdl");
            var cs = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/mapping/cs");

            var element = xmlEntries.Elements(edmx + "Edmx")
                .Elements(edmx + "Runtime")
                .Elements(edmx + "StorageModels")
                .Elements(ssdl + "Schema").First();
            Assert.IsNotNull(element);
            Assert.AreEqual((string)element.Attribute("Namespace"), "SchoolModel.Store");
            Assert.AreEqual((string)element.Attribute("Provider"), "System.Data.SqlClient");

            var entityTypes = (from el in element.Elements(ssdl + "EntityType") select el.Attribute("Name").Value).ToList();

            foreach (var entityName in _entityNames.Where(entityName => !entityName.Contains("Migration")))
            {
                Assert.IsTrue(entityTypes.Any(el => el.Equals(entityName)), string.Format("Looking for Entity name:" + entityName));
            }

            element =
                (from el in
                    element.Elements(ssdl + "EntityType")
                        .Where(el => el.Attribute("Name").Value.Equals("People", StringComparison.CurrentCulture))
                        .Descendants(ssdl + "Property")
                    where (string)el.Attribute("Name") == "PersonID"
                    select el).First();

            Assert.AreEqual((string)element.Attribute("Type"), "int");
            Assert.AreEqual((string)element.Attribute("StoreGeneratedPattern"), "Identity");
            Assert.AreEqual((string)element.Attribute("Nullable"), "false");

            element = xmlEntries.Elements(edmx + "Edmx")
                .Elements(edmx + "Runtime")
                .Elements(edmx + "Mappings")
                .Elements(cs + "Mapping")
                .Elements(cs + "EntityContainerMapping").First();
            Assert.AreEqual((string)element.Attribute("StorageEntityContainer"), "SchoolModelStoreContainer");
            Assert.AreEqual((string)element.Attribute("CdmEntityContainer"), "SchoolEntities");
        }

        private static void CheckFilesCodeFirst(Project project)
        {
            var modelfile = project.GetProjectItemByName("Model1.cs");
            Assert.IsNotNull(modelfile, "modelfile is null");

            var coursfile = project.GetProjectItemByName("Cours.cs");
            Assert.IsNotNull(coursfile, "coursfile is null");

            var departmentfile = project.GetProjectItemByName("Department.cs");
            Assert.IsNotNull(departmentfile, "departmentfile is null");

            var officeassignmentfile = project.GetProjectItemByName("OfficeAssignment.cs");
            Assert.IsNotNull(officeassignmentfile, "officeassignmentfile is null");

            var personfile = project.GetProjectItemByName("Person.cs");
            Assert.IsNotNull(personfile, "personfile is null");

            var studentgradefile = project.GetProjectItemByName("StudentGrade.cs");
            Assert.IsNotNull(studentgradefile, "studentgradefile is null");
        }

        private static void ExecuteSqlCommand(string connectionString, string commandString)
        {
            using (var mycon = new SqlConnection(connectionString))
            {
                using (var mycomm = mycon.CreateCommand())
                {
                    mycomm.CommandText = commandString;
                    mycon.Open();
                    SqlConnection.ClearAllPools();
                    mycomm.ExecuteNonQuery();
                }
            }
        }

        private static string GetRandomProjectName()
        {
            return ProjectPrefix + _projIndex++;
        }

        /// <summary>
        ///     Tries to find the wizard after it is launched.
        /// </summary>
        /// <returns>the wizard element</returns>
        private Window GetWizard(string wizardName)
        {
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":In GetWizard");

            _visualStudio = Application.Attach(Process.GetCurrentProcess().Id);
            _visualStudioMainWindow = _visualStudio.GetWindow(
                SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                InitializeOption.NoCache);

            var wizard = _visualStudio.Find(
                x => x.Equals(_resourceHelper.GetEntityDesignResourceString(wizardName)),
                InitializeOption.NoCache);
            return wizard;
        }

        /// <summary>
        ///     Chooses 'Create Empty Model' option for the wizard and clicks finish button
        /// </summary>
        private void CreateEmptyModel()
        {
            // Select the empty model option
            SelectModelOption(_resourceHelper.GetEntityDesignResourceString("EmptyModelOption"));

            var finish = _wizard.Get<Button>(SearchCriteria.ByText(_resourceHelper.GetWizardFrameworkResourceString("ButtonFinishText")));
            finish.Click();
        }

        /// <summary>
        ///     Chooses the 'Generate from database' option for the wizard and walks thru the wizard
        /// </summary>
        private void CreateModelFromDB(String dbName, Boolean codeFirst)
        {
            SelectModelOption(
                codeFirst
                    ? _resourceHelper.GetEntityDesignResourceString("CodeFirstFromDatabaseOption")
                    : _resourceHelper.GetEntityDesignResourceString("GenerateFromDatabaseOption"));

            ClickNextButton();

            var newConnectionButton =
                _wizard.Get<Button>(
                    SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("NewDatabaseConnectionBtn")));

            newConnectionButton.Click();
            HandleConnectionDialog(dbName);
            _wizard.WaitTill(WaitTillNewDBSettingsLoaded);

            ClickNextButton();

            if (!codeFirst)
            {
                var versionsPanel = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("versionsPanel"));
                var selectionButton =
                    versionsPanel.Get<RadioButton>(
                        SearchCriteria.ByText(
                            String.Format(_resourceHelper.GetEntityDesignResourceString("EntityFrameworkVersionName"), "6.x")));
                Assert.IsTrue(selectionButton.IsSelected);
                ClickNextButton();
            }

            var pane = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("treeView"));
            _wizard.WaitTill(WaitTillDBTablesLoaded, new TimeSpan(0, 1, 0));
            CheckAllCheckBoxes(pane);

            var finishButton = _wizard.Get<Button>(
                SearchCriteria.ByText(_resourceHelper.GetWizardFrameworkResourceString("ButtonFinishText")));
            finishButton.Click();
        }

        /// <summary>
        ///     Walks through the 'Update from database' wizard
        /// </summary>
        private void UpdateModelFromDBWizard()
        {
            _wizard.WaitTill(WaitTillDBChecksLoaded, new TimeSpan(0, 1, 0));

            var pane = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("AddTreeView"));
            CheckAllCheckBoxes(pane);

            var finishButton = _wizard.Get<Button>(
                SearchCriteria.ByAutomationId("_btnFinish"));
            finishButton.Click();
        }

        /// <summary>
        ///     Chooses the 'Generate from database' option for the wizard and walks thru the wizard
        /// </summary>
        private void CreateModelFromDBNonDefaults(string dbName)
        {
            // Select the 'Generate from Database' option
            SelectModelOption(_resourceHelper.GetEntityDesignResourceString("GenerateFromDatabaseOption"));

            ClickNextButton();

            var appconfig = _wizard.Get<TextBox>(SearchCriteria.ByAutomationId("textBoxAppConfigConnectionName"));
            if (appconfig.Text != "SchoolEntities")
            {
                var newConnectionButton =
                    _wizard.Get<Button>(
                        SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("NewDatabaseConnectionBtn")));

                newConnectionButton.Click();
                HandleConnectionDialog(dbName);
                _wizard.WaitTill(WaitTillNewDBSettingsLoaded, new TimeSpan(0, 1, 0));
            }

            ClickNextButton();
            var versionsPanel = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("versionsPanel"));
            var selectionButton =
                versionsPanel.Get<RadioButton>(
                    SearchCriteria.ByText(String.Format(_resourceHelper.GetEntityDesignResourceString("EntityFrameworkVersionName"), "6.x")));
            Assert.IsTrue(selectionButton.IsSelected);
            ClickNextButton();

            _wizard.WaitTill(WaitTillDBTablesLoaded, new TimeSpan(0, 1, 0));
            var pane = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("treeView"));
            CheckAllCheckBoxes(pane);
            ChangeDefaultValues();

            var finishButton = _wizard.Get<Button>(
                SearchCriteria.ByText(_resourceHelper.GetWizardFrameworkResourceString("ButtonFinishText")));
            finishButton.Click();
        }

        private void ChangeDefaultValues()
        {
            _wizard.Get<CheckBox>(SearchCriteria.ByAutomationId("chkPluralize")).Toggle();
            _wizard.Get<CheckBox>(SearchCriteria.ByAutomationId("chkIncludeForeignKeys")).Toggle();
            var functionImports = _wizard.Get<CheckBox>(SearchCriteria.ByAutomationId("chkCreateFunctionImports"));

            if (functionImports.Enabled)
            {
                functionImports.Toggle();
            }

            var modelNameSpace = _wizard.Get<TextBox>(SearchCriteria.ByAutomationId("modelNamespaceTextBox"));
            modelNameSpace.Enter("SchoolContext");
        }

        private void SelectModelOption(string modelOption)
        {
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":SelectModelOption");
            Trace.WriteLine(DateTime.Now.ToLongTimeString() + ":trying to find option list");

            var options =
                _wizard.Get<ListBox>(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("StartPage_PromptLabelText")));
            var item = options.Item(modelOption);
            item.Select();
        }

        // Handles the new connection dialog
        private void HandleConnectionDialog(string dbName)
        {
            // Set button response timeout to longer than it takes for connection string to load
            CoreAppXmlConfiguration.Instance.BusyTimeout = 20000;

            var serverNameText = _wizard.Get<TextBox>(
                SearchCriteria.ByText(
                    _resourceHelper.GetConnectionUIDialogResourceString("serverLabel.Text")));
#if (VS14 || VS15)
            serverNameText.Enter(@"(localdb)\mssqllocaldb");
#else
            serverNameText.Enter(@"(localdb)\v11.0");
#endif

            var refreshButton = _wizard.Get<Button>(
                SearchCriteria.ByText(
                    _resourceHelper.GetConnectionUIDialogResourceString("refreshButton.Text")));
            refreshButton.Focus();

            var dbNameText = _wizard.Get<TextBox>(
                SearchCriteria.ByText(
                    _resourceHelper.GetConnectionUIDialogResourceString("selectDatabaseRadioButton.Text")));
            dbNameText.Enter(dbName);

            var okButton = _wizard.Get<Button>(
                SearchCriteria.ByText(
                    _resourceHelper.GetConnectionUIDialogResourceString("acceptButton.Text")));
            okButton.Click();
        }

        // checks all the checkboxes on the "which database objects to select" dialog
        private static void CheckAllCheckBoxes(UIItemContainer pane)
        {
            var tree = pane.Get<Tree>(SearchCriteria.ByControlType(ControlType.Tree));
            foreach (var checkbox in tree.Nodes)
            {
                checkbox.Click();
                Keyboard.Instance.Enter(" ");
            }
        }

        // Invokes click on next button
        private void ClickNextButton()
        {
            var finish = _wizard.Get<Button>(
                SearchCriteria.ByText(_resourceHelper.GetWizardFrameworkResourceString("ButtonNextText")));
            finish.Click();
        }

        // Ensures the wizard is in the appropriate state before continuing
        private bool WaitTillNewDBSettingsLoaded()
        {
            var appconfig = _wizard.Get<TextBox>(SearchCriteria.ByAutomationId("textBoxAppConfigConnectionName"));
            return appconfig.Text.Equals("Model1") || appconfig.Text.Equals("SchoolEntities");
        }

        private bool WaitTillDBTablesLoaded()
        {
            var pane = _wizard.Get<Panel>(SearchCriteria.ByAutomationId("treeView"));
            var tree = pane.Get<Tree>(SearchCriteria.ByControlType(ControlType.Tree));
            return (tree.Nodes.Count != 0);
        }

        private bool WaitTillDBChecksLoaded()
        {
            var modelNamespaceTextBox = _wizard.Get<TextBox>(SearchCriteria.ByAutomationId("DescriptionTextBox"));
            return modelNamespaceTextBox.Visible;
        }

        public List<string> AddEntities()
        {
            var entities = new List<string>();
            var designerSurface = _visualStudioMainWindow.Get<Panel>(SearchCriteria.ByText("Modeling Design Surface"));
            entities.Add(AddEntity(designerSurface));
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");

            entities.Add(AddEntity(designerSurface));
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");

            entities.Add(AddEntity(designerSurface));
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");

            entities.Add(AddEntity(designerSurface));
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");

            return entities;
        }

        public void AddAssociations(List<string> entities)
        {
            var designerSurface = _visualStudioMainWindow.Get<Panel>(SearchCriteria.ByText("Modeling Design Surface"));
            AddAssociation(designerSurface, entities[0], entities[1], AssociationType.OneToOne);
            AddAssociation(designerSurface, entities[2], entities[3], AssociationType.OneToMany);
            AddAssociation(designerSurface, entities[0], entities[2], AssociationType.ManyToMany);
        }

        private string AddEntity(IUIItem designerSurface)
        {
            designerSurface.RightClickAt(designerSurface.Location);
            var popUpMenu = _visualStudioMainWindow.Popup;
            var menuItem = popUpMenu.Item("Add New");
            menuItem.Click();
            var newEntity =
                menuItem.SubMenu(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("AddEntityTypeCommand_DesignerText")));
            newEntity.Click();
            var addEntity = _visualStudio.Find(
                x => x.Equals(_resourceHelper.GetEntityDesignResourceString("NewEntityDialog_Title")),
                InitializeOption.NoCache);

            var entityName =
                addEntity.Get<TextBox>(
                    SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("NewEntityDialog_EntityNameLabel")));
            var entityNameText = string.Format("Entity_{0}", _nameSuffix++);
            entityName.Text = entityNameText;
            var okButton =
                addEntity.Get<Button>(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("OKButton_AccessibleName")));
            okButton.Click();
            Dte.ExecuteCommand("OtherContextMenus.MicrosoftDataEntityDesignContext.AddNew.ScalarProperty");
            return entityNameText;
        }

        private void AddAssociation(IUIItem designerSurface, string entity1, string entity2, AssociationType associationType)
        {
            designerSurface.RightClickAt(designerSurface.Location);
            var popUpMenu = _visualStudioMainWindow.Popup;
            var menuItem = popUpMenu.Item("Add New");
            menuItem.Click();
            var newAssociation = menuItem.SubMenu(SearchCriteria.ByText("Association..."));
            newAssociation.Click();
            var addAssociation = _visualStudio.Find(
                x => x.Equals(_resourceHelper.GetEntityDesignResourceString("NewAssociationDialog_Title")),
                InitializeOption.NoCache);

            var entity1Combo = addAssociation.Get<ComboBox>(SearchCriteria.ByAutomationId("entity1ComboBox"));
            entity1Combo.Select(entity1);

            var entity2Combo = addAssociation.Get<ComboBox>(SearchCriteria.ByAutomationId("entity2ComboBox"));
            entity2Combo.Select(entity2);

            var multiplicity1ComboBox = addAssociation.Get<ComboBox>(SearchCriteria.ByAutomationId("multiplicity1ComboBox"));
            var multiplicity2ComboBox = addAssociation.Get<ComboBox>(SearchCriteria.ByAutomationId("multiplicity2ComboBox"));
            switch (associationType)
            {
                case AssociationType.OneToOne:
                    multiplicity1ComboBox.Select(0);
                    multiplicity2ComboBox.Select(0);
                    break;
                case AssociationType.OneToMany:
                    multiplicity1ComboBox.Select(0);
                    multiplicity2ComboBox.Select(2);
                    break;
                case AssociationType.ManyToMany:
                    multiplicity1ComboBox.Select(2);
                    multiplicity2ComboBox.Select(2);
                    break;
            }

            var okButton =
                addAssociation.Get<Button>(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("OKButton_AccessibleName")));
            okButton.Click();
        }

        private void AddInheritance(IUIItem designerSurface, string baseEntity, string derivedEntity)
        {
            designerSurface.RightClickAt(designerSurface.Location);
            var popUpMenu = _visualStudioMainWindow.Popup;
            var menuItem = popUpMenu.Item("Add New");
            menuItem.Click();
            var newInheritance = menuItem.SubMenu(SearchCriteria.ByText("Inheritance..."));
            newInheritance.Click();
            var addInheritance = _visualStudio.Find(
                x => x.Equals(_resourceHelper.GetEntityDesignResourceString("NewInheritanceDialog_Title")),
                InitializeOption.NoCache);
            var entity1 =
                addInheritance.Get<ComboBox>(
                    SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("NewInheritanceDialog_SelectBaseEntity")));
            entity1.Item(baseEntity).Select();
            var entity2 =
                addInheritance.Get<ComboBox>(
                    SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("NewInheritanceDialog_SelectDerivedEntity")));
            entity2.Item(derivedEntity).Select();
            var okButton =
                addInheritance.Get<Button>(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("OKButton_AccessibleName")));
            okButton.Click();
        }

        private void AddEnum(IUIItem designerSurface)
        {
            designerSurface.RightClickAt(designerSurface.Location);
            var popUpMenu = _visualStudioMainWindow.Popup;
            var menuItem = popUpMenu.Item("Add New");
            menuItem.Click();
            var newEnum =
                menuItem.SubMenu(SearchCriteria.ByText(_resourceHelper.GetEntityDesignResourceString("AddEnumTypeCommand_DesignerText")));
            newEnum.Click();
            var addEnum = _visualStudio.Find(
                x => x.Equals(_resourceHelper.GetEntityDesignResourceString("EnumDialog_NewEnumWindowTitle")),
                InitializeOption.NoCache);
            var enumTypeName = addEnum.Get<TextBox>(SearchCriteria.ByAutomationId("txtEnumTypeName"));
            enumTypeName.SetValue("EnumType" + _nameSuffix);

            var table = addEnum.Get<ListView>(SearchCriteria.ByAutomationId("dgEnumTypeMembers"));

            foreach (var row in table.Rows)
            {
                row.Select();
                row.DoubleClick();
                row.Focus();
            }

            var rowEntry =
                table.Get<UIItem>(
                    SearchCriteria.ByText(
                        "Item: Microsoft.Data.Entity.Design.UI.ViewModels.EnumTypeMemberViewModel, Column Display Index: 0"));
            var rowEntryValuePattern =
                (ValuePattern)
                    rowEntry.AutomationElement.GetCurrentPattern(ValuePattern.Pattern);
            rowEntryValuePattern.SetValue("EnumName" + _nameSuffix++);

            var btnOk = addEnum.Get<Button>(SearchCriteria.ByAutomationId("btnOk"));
            btnOk.Click();
        }

        // Creates a new thread for action and starts it
        private static SystemThread ExecuteThreadedAction(Action action, string threadName = "Worker")
        {
            var thread = new SystemThread(new ThreadStart(action)) { Name = threadName };
            thread.Start();
            return thread;
        }

        /// <summary>
        ///     Create a simple db for tests to use
        /// </summary>
        private static void CreateDatabase()
        {
            using (var db = new SchoolEntities())
            {
                db.Database.Initialize(false);
            }

            SqlConnection.ClearAllPools();
        }

        /// <summary>
        ///     Create a simple db for tests to use
        /// </summary>
        private static void DropDatabase()
        {
            using (var db = new SchoolEntities())
            {
                if (db.Database.Exists())
                {
                    db.Database.Delete();
                }
            }
        }

        public static ErrorItems Build()
        {
            Dte.ExecuteCommand("Build.BuildSelection", String.Empty);

            // Wait for the build to be completed
            while (Dte.Solution.SolutionBuild.BuildState != vsBuildState.vsBuildStateDone)
            {
                SystemThread.Sleep(100);
            }

            var errors = GetErrors();

            return errors;
        }

        public static ErrorItems GetErrors()
        {
            var dte2 = (DTE2)Dte;
            Dte.ExecuteCommand("View.ErrorList", string.Empty);
            var errorItems = dte2.ToolWindows.ErrorList.ErrorItems;
            if (errorItems == null
                || errorItems.Count == 0)
            {
                return errorItems;
            }
            Trace.WriteLine(string.Format("THere are {0} Build Errors", errorItems.Count));
            for (var i = 1; i <= errorItems.Count; i++)
            {
                Trace.WriteLine(
                    string.Format(
                        "File: {0}\tDescription:{1}\tLine:{2}", errorItems.Item(i).FileName, errorItems.Item(i).Description,
                        errorItems.Item(i).Line));
            }

            return errorItems;
        }
    }
}

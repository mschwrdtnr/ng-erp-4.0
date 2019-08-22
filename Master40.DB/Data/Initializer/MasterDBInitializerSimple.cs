using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.DataModel;

namespace Master40.DB.Data.Initializer
{
    public static class MasterDBInitializerSimple
    {
        public static void DbInitialize(MasterDBContext context)
        {
            context.Database.EnsureCreated();


            // Look for any Entrys.
            if (context.Articles.Any())
            {
                return;   // DB has been seeded
            }
            // Article Types
            var articleTypes = new M_ArticleType[]
            {
                new M_ArticleType {Name = "Assembly"},
                new M_ArticleType {Name = "Material"},
                new M_ArticleType {Name = "Consumable"},
                new M_ArticleType {Name = "Product"}
            };

            context.ArticleTypes.AddRange(entities: articleTypes);
            context.SaveChanges();

            // Units
            var units = new M_Unit[]
            {
                new M_Unit {Name = "Kilo"},
                new M_Unit {Name = "Litre"},
                new M_Unit {Name = "Pieces"}
            };
            context.Units.AddRange(entities: units);
            context.SaveChanges();

            var resourceSkills = new M_ResourceSkill[]
            {
                new M_ResourceSkill { Name = "CuttingSkill", ResourceSetups = new List<M_ResourceSetup>() },
                new M_ResourceSkill { Name = "DrillSkill", ResourceSetups = new List<M_ResourceSetup>() },
                new M_ResourceSkill { Name = "AssemblyUnitSkill", ResourceSetups = new List<M_ResourceSetup>()},
            };

            var resourceTools = new M_ResourceTool[]
            {
                new M_ResourceTool{ Name="Saw blade big", ResourceSetups = new List<M_ResourceSetup>()},
                new M_ResourceTool{ Name="Saw blade small", ResourceSetups = new List<M_ResourceSetup>()},
                new M_ResourceTool{ Name="M6 head", ResourceSetups = new List<M_ResourceSetup>()},
                new M_ResourceTool{ Name="M4 head", ResourceSetups = new List<M_ResourceSetup>()},
                new M_ResourceTool{ Name="Screwdriver universal cross size 2", ResourceSetups = new List<M_ResourceSetup>()},
            };
            context.ResourceTools.AddRange(entities: resourceTools);
            context.SaveChanges();

            var resources = new M_Resource[] {
                new M_Resource{Name="Saw 1", Count = 1, ResourceSetups = new List<M_ResourceSetup>() },
                new M_Resource{Name="Saw 2", Count = 1, ResourceSetups = new List<M_ResourceSetup>()},
                new M_Resource{Name="Drill 1", Count = 1 , ResourceSetups = new List<M_ResourceSetup>()},
                new M_Resource{Name="AssemblyUnit 1", Count = 1, ResourceSetups = new List<M_ResourceSetup>()},
                new M_Resource{Name="AssemblyUnit 2", Count = 1, ResourceSetups = new List<M_ResourceSetup>()},
            };
            context.Resources.AddRange(entities: resources);
            context.SaveChanges();

            var resourceSetups = new M_ResourceSetup[]{
            new M_ResourceSetup { Name = "Saw1_Sawbladebig", Resource = resources.Single(predicate: s => s.Name == "Saw 1"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "Saw blade big"), SetupTime = 5 },
            new M_ResourceSetup { Name = "Saw2_Sawbladebig", Resource = resources.Single(predicate: s => s.Name == "Saw 2"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "Saw blade big"), SetupTime = 5 },
            new M_ResourceSetup { Name = "Saw2_Sawbladesmall", Resource = resources.Single(predicate: s => s.Name == "Saw 2"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "Saw blade small"), SetupTime = 5 },

            new M_ResourceSetup { Name = "Drill1_M4", Resource = resources.Single(predicate: s => s.Name == "Drill 1"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "M4 head"), SetupTime = 10 },
            new M_ResourceSetup { Name = "Drill1_M6", Resource = resources.Single(predicate: s => s.Name == "Drill 1"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "M6 head"), SetupTime = 10 },

            new M_ResourceSetup { Name = "AssemblyUnit1_Screwdriver universal cross size 2", Resource = resources.Single(predicate: s => s.Name == "AssemblyUnit 1"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "Screwdriver universal cross size 2"), SetupTime = 5 },
            new M_ResourceSetup { Name = "AssemblyUnit2_Screwdriver universal cross size 2", Resource = resources.Single(predicate: s => s.Name == "AssemblyUnit 2"), ResourceTool = resourceTools.Single(predicate: s => s.Name == "Screwdriver universal cross size 2"), SetupTime = 5 },
            };

            // register resourceToResourceTool at the resourceSkill
            resourceSkills.Single(predicate: s => s.Name == "CuttingSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "Saw1_Sawbladebig"));
            resourceSkills.Single(predicate: s => s.Name == "CuttingSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "Saw2_Sawbladebig"));
            resourceSkills.Single(predicate: s => s.Name == "CuttingSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "Saw2_Sawbladesmall"));

            resourceSkills.Single(predicate: s => s.Name == "DrillSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "Drill1_M4"));
            resourceSkills.Single(predicate: s => s.Name == "DrillSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "Drill1_M6"));

            resourceSkills.Single(predicate: s => s.Name == "AssemblyUnitSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "AssemblyUnit1_Screwdriver universal cross size 2"));
            resourceSkills.Single(predicate: s => s.Name == "AssemblyUnitSkill").ResourceSetups.Add(item: resourceSetups.Single(predicate: r => r.Name == "AssemblyUnit2_Screwdriver universal cross size 2"));

            context.ResourceSkills.AddRange(entities: resourceSkills);
            context.ResourceSetups.AddRange(entities: resourceSetups);
            context.SaveChanges();

            // Articles
            var articles = new M_Article[]
            {
                // Final Products
                new M_Article{Name="Tisch",  ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Product").Id, CreationDate = DateTime.Parse(s: "2016-09-01"), DeliveryPeriod = 20, UnitId = units.Single( predicate: s => s.Name == "Pieces").Id, Price = 25.00, ToPurchase = false, ToBuild = true, PictureUrl = "/images/Product/05_Truck_final.jpg"},

                // Intermediate Products
                new M_Article{Name="Tischbein",  ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Assembly").Id, CreationDate = DateTime.Parse(s: "2016-09-01"), DeliveryPeriod = 5, UnitId = units.Single( predicate: s => s.Name == "Pieces").Id, Price = 2.00, ToPurchase = false, ToBuild = true, PictureUrl = "/images/Product/05_Truck_final.jpg"},
                new M_Article{Name="Tischplatte",  ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Assembly").Id, CreationDate = DateTime.Parse(s: "2016-09-01"), DeliveryPeriod = 5, UnitId = units.Single( predicate: s => s.Name == "Pieces").Id, Price = 10.00, ToPurchase = false, ToBuild = true, PictureUrl = "/images/Product/05_Truck_final.jpg"},
                
                // base Materials
                new M_Article{Name="Holzplatte 1,5m x 3,0m x 0,03m", ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Material").Id, CreationDate = DateTime.Parse(s: "2002-09-01"), DeliveryPeriod = 5, UnitId = units.Single( predicate: s => s.Name == "Pieces").Id, Price = 3.00, ToPurchase = true, ToBuild = false},
                new M_Article{Name="Holzpflock 1,20m x 0,15m x 0,15m", ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Material").Id, CreationDate = DateTime.Parse(s: "2002-09-01"), DeliveryPeriod = 5, UnitId = units.Single( predicate: s => s.Name == "Pieces").Id, Price = 0.70, ToPurchase = true, ToBuild = false},
                new M_Article{Name="Schrauben", ArticleTypeId = articleTypes.Single( predicate: s => s.Name == "Consumable").Id, CreationDate = DateTime.Parse(s: "2005-09-01"), DeliveryPeriod  = 4, UnitId = units.Single( predicate: s => s.Name == "Kilo").Id, Price = 0.50, ToPurchase = true, ToBuild = false},

            };

            context.Articles.AddRange(entities: articles);
            context.SaveChanges();

            // create Stock Entrys for each Article
            foreach (var article in articles)
            {
                var stock = new M_Stock
                {
                    ArticleForeignKey = article.Id,
                    Name = "Stock: " + article.Name,
                    Min = (article.ToPurchase) ? 1000 : 0,
                    Max = (article.ToPurchase) ? 2000 : 0,
                    Current = (article.ToPurchase) ? 1000 : 0,
                    StartValue = (article.ToPurchase) ? 1000 : 0,
                };
                context.Stocks.Add(entity: stock);
                context.SaveChanges();
            }
            var operations = new M_Operation[]
            {
                // Final Product Tisch
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tisch").Id, Name = "Tisch zusammenstellen", Duration=5,ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="AssemblyUnitSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="Screwdriver universal cross size 2"), HierarchyNumber = 10 },
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tisch").Id, Name = "Tisch verschrauben", Duration=20, ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="AssemblyUnitSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="Screwdriver universal cross size 2"), HierarchyNumber = 20 },

                // Bom For Tischbein
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tischbein").Id, Name = "Tischbein saegen", Duration=10, ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="CuttingSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="Saw blade big"), HierarchyNumber = 10 },
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tischbein").Id, Name = "Tischbein bohren", Duration=5, ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="DrillSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="M6 head"), HierarchyNumber = 20 },

                // Bom For Tischplatte
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tischplatte").Id, Name = "Tischplatte saegen", Duration=40, ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="CuttingSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="Saw blade small"), HierarchyNumber = 10 },
                new M_Operation{  ArticleId = articles.Single(predicate: a => a.Name == "Tischplatte").Id, Name = "Tischplatte bohren", Duration=5, ResourceSkill=resourceSkills.Single(predicate: s => s.Name =="DrillSkill"), ResourceTool=resourceTools.Single(predicate: t => t.Name =="M6 head"), HierarchyNumber = 20 },


            };
            context.Operations.AddRange(entities: operations);
            context.SaveChanges();

            // !!! - Important NOTE - !!!
            // For Boms without Link to an Opperation all Materials have to be ready to compleate the opperation assignet to the Article.
            var articleBom = new List<M_ArticleBom>
            {
                // Final Product Tisch 
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Tisch").Id, Name = "Tisch" },

                // Bom For Tisch
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Tischbein").Id, Name = "Tischbein", Quantity=4, ArticleParentId = articles.Single(predicate: a => a.Name == "Tisch").Id, OperationId = operations.Single(predicate: x => x.Name == "Tisch verschrauben").Id },
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Tischplatte").Id, Name = "Tischplatte", Quantity=1, ArticleParentId = articles.Single(predicate: a => a.Name == "Tisch").Id, OperationId = operations.Single(predicate: x => x.Name == "Tisch zusammenstellen").Id },
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Schrauben").Id, Name = "Schrauben", Quantity=8, ArticleParentId = articles.Single(predicate: a => a.Name == "Tisch").Id, OperationId = operations.Single(predicate: x => x.Name == "Tisch zusammenstellen").Id },
              
                // Bom For Tischplatte
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Holzplatte 1,5m x 3,0m x 0,03m").Id, Name = "Holzplatte 1,5m x 3,0m x 0,03m", Quantity=1, ArticleParentId = articles.Single(predicate: a => a.Name == "Tischplatte").Id, OperationId = operations.Single(predicate: x => x.Name == "Tischplatte saegen").Id },

                // Bom For Tischbein
                new M_ArticleBom { ArticleChildId = articles.Single(predicate: a => a.Name == "Holzpflock 1,20m x 0,15m x 0,15m").Id, Name = "Holzpflock 1,20m x 0,15m x 0,15m", Quantity=1, ArticleParentId = articles.Single(predicate: a => a.Name == "Tischbein").Id, OperationId = operations.Single(predicate: x => x.Name == "Tischbein saegen").Id },

            };
            context.ArticleBoms.AddRange(entities: articleBom);
            context.SaveChanges();


            //create Businesspartner
            var businessPartner = new M_BusinessPartner() { Debitor = true, Kreditor = false, Name = "Toys'R'us toy department" };
            var businessPartner2 = new M_BusinessPartner() { Debitor = false, Kreditor = true, Name = "Material wholesale" };
            context.BusinessPartners.Add(entity: businessPartner);
            context.BusinessPartners.Add(entity: businessPartner2);
            context.SaveChanges();

            var artToBusinessPartner = new M_ArticleToBusinessPartner[]
            {
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Tisch").Id,PackSize = 10,Price = 20.00, TimeToDelivery = 2880},
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Tischbein").Id,PackSize = 10,Price = 20.00, TimeToDelivery = 2880},
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Tischplatte").Id, PackSize = 500,Price = 0.05, TimeToDelivery = 1440},
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Schrauben").Id, PackSize = 50,Price = 2.50, TimeToDelivery = 1440},
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Holzpflock 1,20m x 0,15m x 0,15m").Id, PackSize = 50,Price = 0.20, TimeToDelivery = 1440},
                new M_ArticleToBusinessPartner{ BusinessPartnerId = businessPartner2.Id, ArticleId = articles.Single(predicate: x => x.Name=="Holzplatte 1,5m x 3,0m x 0,03m").Id, PackSize = 50,Price = 0.20, TimeToDelivery = 1440},

            };
            context.ArticleToBusinessPartners.AddRange(entities: artToBusinessPartner);
            context.SaveChanges();

        }
    }
}
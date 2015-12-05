﻿using System.Linq;
using System.Xml.Linq;
using Orchard.Data;
using Orchard.Layouts.Models;
using Orchard.Localization;
using Orchard.Recipes.Services;
using Orchard.Layouts.Services;
using Orchard.Layouts.Helpers;
using Orchard.Layouts.Framework.Drivers;

namespace Orchard.Layouts.Recipes.Builders {

    public class CustomElementsStep : RecipeBuilderStep {
        private readonly IRepository<ElementBlueprint> _repository;
        private readonly IElementManager _elementManager;

        public CustomElementsStep(IRepository<ElementBlueprint> repository, IElementManager elementManager) {
            _repository = repository;
            _elementManager = elementManager;
        }

        public override string Name {
            get { return "CustomElements"; }
        }

        public override LocalizedString DisplayName {
            get { return T("Custom Elements"); }
        }

        public override LocalizedString Description {
            get { return T("Exports custom defined elements."); }
        }

        public override void Build(BuildContext context)
        {
            var blueprints = _repository.Table.OrderBy(x => x.ElementTypeName).ToList();

            if (!blueprints.Any())
                return;

            var blueprintEntries = blueprints.Select(blueprint => {

                var describeContext = DescribeElementsContext.Empty;
                var descriptor = _elementManager.GetElementDescriptorByTypeName(describeContext, blueprint.BaseElementTypeName);
                var baseElement = _elementManager.ActivateElement(descriptor);
                baseElement.Data = ElementDataHelper.Deserialize(blueprint.BaseElementState);
                return new { Blueprint = blueprint, BaseElement = baseElement };

            }).ToList();

            var baseElements = blueprintEntries.Select(e => e.BaseElement).ToList();
            var exportLayoutContext = new ExportLayoutContext();
            _elementManager.Exporting(baseElements, exportLayoutContext);
            _elementManager.Exported(baseElements, exportLayoutContext);


            var root = new XElement("CustomElements");
            context.RecipeDocument.Element("Orchard").Add(root);

            foreach (var bluprintEntry in blueprintEntries) {

                var xmlElement = new XElement("Element",
                    new XAttribute("ElementTypeName", bluprintEntry.Blueprint.ElementTypeName),
                    new XAttribute("BaseElementTypeName", bluprintEntry.Blueprint.BaseElementTypeName),
                    new XAttribute("ElementDisplayName", bluprintEntry.Blueprint.ElementDisplayName),
                    new XAttribute("BaseExportableData", bluprintEntry.BaseElement.ExportableData.Serialize()),
                    new XElement("BaseElementState", new XCData(bluprintEntry.Blueprint.BaseElementState)));

                if (bluprintEntry.Blueprint.ElementDescription != null)
                    xmlElement.Add(new XAttribute("ElementDescription", bluprintEntry.Blueprint.ElementDescription));

                if (bluprintEntry.Blueprint.ElementCategory != null)
                    xmlElement.Add(new XAttribute("ElementCategory", bluprintEntry.Blueprint.ElementCategory));

                root.Add(xmlElement);
            }
        }
    }
}


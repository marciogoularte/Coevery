﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Coevery.ContentManagement;
using Coevery.Data;
using Coevery.Localization;
using Coevery.Logging;
using Coevery.Recipes.Models;
using Coevery.Recipes.Services;

namespace Coevery.Recipes.RecipeHandlers {
    public class DataRecipeHandler : IRecipeHandler {
        private readonly ICoeveryServices _coeveryServices;
        private readonly ITransactionManager _transactionManager;

        public DataRecipeHandler(ICoeveryServices coeveryServices, ITransactionManager transactionManager) {
            _coeveryServices = coeveryServices;
            _transactionManager = transactionManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        // <Data />
        // Import Data
        public void ExecuteRecipeStep(RecipeContext recipeContext) {
            if (!String.Equals(recipeContext.RecipeStep.Name, "Data", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            var importContentSession = new ImportContentSession(_coeveryServices.ContentManager);

            // Populate local dictionary with elements and their ids
            var elementDictionary = CreateElementDictionary(recipeContext.RecipeStep.Step);

            //Populate import session with all identities to be imported
            foreach (var identity in elementDictionary.Keys) {
                importContentSession.Set(identity.ToString(), elementDictionary[identity].Name.LocalName);
            }

            //Determine if the import is to be batched in multiple transactions
            var startIndex = 0;
            int batchSize = GetBatchSizeForDataStep(recipeContext.RecipeStep.Step);

            //Run the import
            ContentIdentity nextIdentity = null;
            try {
                while (startIndex < elementDictionary.Count) {
                    importContentSession.InitializeBatch(startIndex, batchSize);

                    //the session determines which items are included in the current batch
                    //so that dependencies can be managed within the same transaction
                    nextIdentity = importContentSession.GetNextInBatch();
                    while (nextIdentity != null) {
                        _coeveryServices.ContentManager.Import(elementDictionary[nextIdentity], importContentSession);
                        nextIdentity = importContentSession.GetNextInBatch();
                    }

                    startIndex += batchSize;

                    //Create a new transaction for each batch
                    if (startIndex < elementDictionary.Count) {
                        _coeveryServices.ContentManager.Clear();
                        _transactionManager.RequireNew();
                    }
                }
            }
            catch (Exception) {
                //Ensure a failed batch is rolled back
                _transactionManager.Cancel();
                throw;
            }

            recipeContext.Executed = true;
        }

        private Dictionary<ContentIdentity, XElement> CreateElementDictionary(XElement step) {
            var elementDictionary = new Dictionary<ContentIdentity, XElement>(new ContentIdentity.ContentIdentityEqualityComparer());
            foreach (var element in step.Elements()) {
                if (element.Attribute("Id") == null || string.IsNullOrEmpty(element.Attribute("Id").Value))
                    continue;

                var identity = new ContentIdentity(element.Attribute("Id").Value);
                elementDictionary[identity] = element;
            }
            return elementDictionary;
        }

        private int GetBatchSizeForDataStep(XElement step) {
            int batchSize;
            if (step.Attribute("BatchSize") == null ||
                !int.TryParse(step.Attribute("BatchSize").Value, out batchSize) ||
                batchSize <= 0) {
                batchSize = int.MaxValue;
            }
            return batchSize;
        }
    }
}

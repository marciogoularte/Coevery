﻿using System.Collections.Generic;
using Coevery.Entities.Services;
using Coevery.OptionSet.Models;
using Coevery.ContentManagement;
using Coevery.ContentManagement.Utilities;

namespace Coevery.OptionSet.Fields {
    /// <summary>
    /// This field has not state, as all terms are saved using <see cref="TermContentItem"/>
    /// </summary>
    public class OptionSetField : ContentField {
        internal LazyField<IEnumerable<OptionItemPart>> OptionItemsField { get; set; }

        public OptionSetField() {
            OptionItemsField = new LazyField<IEnumerable<OptionItemPart>>();
        }

        /// <summary>
        /// Gets the Terms associated with this field
        /// </summary>
        public IEnumerable<OptionItemPart> OptionItems {
            get { return OptionItemsField.Value; }
        }
    }
}
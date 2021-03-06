﻿using System;
using System.Linq.Expressions;
using System.Xml.Linq;
using Coevery.ContentManagement.FieldStorage.InfosetStorage;
using Coevery.Utility;

namespace Coevery.ContentManagement {
    public static class InfosetHelper {

        public static TProperty Retrieve<TPart, TProperty>(this TPart contentPart,
            Expression<Func<TPart, TProperty>> targetExpression,
            bool versioned = false) where TPart : ContentPart {

            var propertyInfo = ReflectionHelper<TPart>.GetPropertyInfo(targetExpression);
            var name = propertyInfo.Name;

            var infosetPart = contentPart.As<InfosetPart>();
            var el = infosetPart == null
                ? null
                : (versioned ? infosetPart.VersionInfoset.Element : infosetPart.Infoset.Element)
                .Element(contentPart.GetType().Name);
            return el == null ? default(TProperty) : el.Attr<TProperty>(name);
        }

        public static TProperty Retrieve<TProperty>(this ContentPart contentPart, string name, 
            bool versioned = false) {
            var infosetPart = contentPart.As<InfosetPart>();
            var el = infosetPart == null
                ? null
                : (versioned ? infosetPart.VersionInfoset.Element : infosetPart.Infoset.Element)
                .Element(contentPart.GetType().Name);
            return el == null ? default(TProperty) : el.Attr<TProperty>(name);
        }

        public static void Store<TPart, TProperty>(this TPart contentPart, 
            Expression<Func<TPart, TProperty>> targetExpression,
            TProperty value, bool versioned = false) where TPart : ContentPart {

            var partName = contentPart.GetType().Name;
            var infosetPart = contentPart.As<InfosetPart>();
            var propertyInfo = ReflectionHelper<TPart>.GetPropertyInfo(targetExpression);
            var name = propertyInfo.Name;

            Store(infosetPart, partName, name, value, versioned);
        }

        public static void Store<TProperty>(this ContentPart contentPart, string name, 
            TProperty value, bool versioned = false) {

            var partName = contentPart.GetType().Name;
            var infosetPart = contentPart.As<InfosetPart>();
           
            Store(infosetPart, partName, name, value, versioned);
        }

        public static void Store<TProperty>(this InfosetPart infosetPart, string partName, string name, TProperty value, bool versioned = false) {
            
            var infoset = (versioned ? infosetPart.VersionInfoset : infosetPart.Infoset);
            var partElement = infoset.Element.Element(partName);
            if (partElement == null) {
                partElement = new XElement(partName);
                infoset.Element.Add(partElement);
            }
            partElement.Attr(name, value);
        }

        public static TProperty Retrieve<TPart, TRecord, TProperty>(this TPart contentPart,
            Expression<Func<TRecord, TProperty>> targetExpression)
            where TPart : ContentPart<TRecord> {

            var getter = ReflectionHelper<TRecord>.GetGetter(targetExpression);
            return contentPart.Retrieve(targetExpression, getter);
        }

        public static TProperty Retrieve<TPart, TRecord, TProperty>(this TPart contentPart,
            Expression<Func<TRecord, TProperty>> targetExpression,
            Delegate defaultExpression)
            where TPart : ContentPart<TRecord> {

            var propertyInfo = ReflectionHelper<TRecord>.GetPropertyInfo(targetExpression);
            var name = propertyInfo.Name;

            var infosetPart = contentPart.As<InfosetPart>();
            var el = infosetPart == null
                ? null
                : infosetPart.Infoset.Element.Element(contentPart.GetType().Name);
            if (el == null || el.Attribute(name) == null) {
                // Property has never been stored. Get it from the default expression and store that.
                var defaultValue = defaultExpression == null
                    ? default(TProperty)
                    : (TProperty)defaultExpression.DynamicInvoke(contentPart.Record);
                contentPart.Store(name, defaultValue);
                return defaultValue;
            }
            return el.Attr<TProperty>(name);
        }

        public static void Store<TPart, TRecord, TProperty>(this TPart contentPart,
            Expression<Func<TRecord, TProperty>> targetExpression,
            TProperty value)
            where TPart : ContentPart<TRecord> {

            var propertyInfo = ReflectionHelper<TRecord>.GetPropertyInfo(targetExpression);
            var name = propertyInfo.Name;
            propertyInfo.SetValue(contentPart.Record, value, null);
            contentPart.Store(name, value);
        }
    }
}

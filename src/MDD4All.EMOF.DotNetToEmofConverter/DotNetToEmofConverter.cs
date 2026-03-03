using MDD4All.DataModeling.Attributes;
using MDD4All.EMOF.DataModels;
using MDD4All.EMOF.DataModels.Base;
using MDD4All.EMOF.DataModels.Enumerations;
using MDD4All.EMOF.DataModels.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MDD4All.EMOF.DotNetToEmofConverter
{
    public class DotNetToEmofConverter
    {

        public EmofRepository ConvertToEMOF(Type type)
        {
            Type[] assemblyTypes = Assembly.GetAssembly(type).GetTypes();

            EmofRepository result = new EmofRepository();

            foreach (Type assemblyType in assemblyTypes)
            {
                GetOrCreateElementRecursively(assemblyType, result);
            }

            return result;
        }

        private PackageableElement? GetOrCreateElementRecursively(Type type, EmofRepository repository)
        {
            PackageableElement? result = null;

            string? namespaceTitle = type.Namespace;
            if(namespaceTitle == null)
            {
                namespaceTitle = "";
            }

            Package? package = GetOrCreatePackageForNamespace(namespaceTitle, repository);

            if (package != null)
            {
                bool isTemplate = false;
                string name = type.Name;
                if (name.Contains("`")) // generic type
                {
                    string[] parts = name.Split('`');
                    name = parts[0];
                    isTemplate = true;
                }

                PackageableElement? packageableElement = package.PackagedElements.FirstOrDefault(element => element.Name == name);

                VisibilityKind visibility = VisibilityKind.Private;

                if (name.ToLower().Contains("property"))
                {
                    ;
                }

                if (packageableElement == null)
                {
                    if (type.IsClass || (type.IsValueType && !type.IsEnum))
                    {
                        packageableElement = new Class()
                        {
                            Name = name,
                            OwningPackage = package,
                            IsAbstract = type.IsAbstract
                        };

                        package.PackagedElements.Add(packageableElement);

                        if (isTemplate)
                        {
                            ((Class)packageableElement).OwnedTemplateSignature = new TemplateSignature();
                        }

                        // inheritance
                        if (type.BaseType != null)
                        {
                            PackageableElement? baseTypeElement = GetOrCreateElementRecursively(type.BaseType, repository);

                            if (baseTypeElement != null)
                            {
                                if (((Class)packageableElement).SuperClassRefs == null)
                                {
                                    ((Class)packageableElement).SuperClassRefs = new List<string>();
                                }

                                ((Class)packageableElement).SuperClassRefs!.Add(baseTypeElement.FullName);
                            }
                        }

                        // implemented interfaces
                        Type[] implementedInterfaceTypes = GetDirectInterfaces(type);
                        foreach (Type implementedInterfaceType in implementedInterfaceTypes)
                        {
                            PackageableElement? baseTypeElement = GetOrCreateElementRecursively(implementedInterfaceType, repository);

                            if (baseTypeElement != null)
                            {
                                if (((Class)packageableElement).SuperClassRefs == null)
                                {
                                    ((Class)packageableElement).SuperClassRefs = new List<string>();
                                }

                                ((Class)packageableElement).SuperClassRefs!.Add(baseTypeElement.FullName);
                            }
                        }


                        

                        AddProperties(package, packageableElement, type, repository);

                    }
                    else if (type.IsInterface)
                    {
                        packageableElement = new Interface
                        {
                            Name = name,
                            OwningPackage = package
                        };

                        package.PackagedElements.Add(packageableElement);

                        if (isTemplate)
                        {
                            SetTemplateSettings((Classifier)packageableElement, type, repository);
                        }

                        // inheritance
                        if (type.BaseType != null)
                        {
                            PackageableElement? baseTypeElement = GetOrCreateElementRecursively(type.BaseType, repository);

                            if (baseTypeElement != null)
                            {
                                if (((Interface)packageableElement).RedefinedInterfacesRefs == null)
                                {
                                    ((Interface)packageableElement).RedefinedInterfacesRefs = new List<string>();
                                }

                                ((Interface)packageableElement).RedefinedInterfacesRefs!.Add(baseTypeElement.FullName);
                            }
                        }

                        // implemented interfaces
                        Type[] implementedInterfaceTypes = GetDirectInterfaces(type);
                        foreach (Type implementedInterfaceType in implementedInterfaceTypes)
                        {
                            PackageableElement? baseTypeElement = GetOrCreateElementRecursively(implementedInterfaceType, repository);

                            if (baseTypeElement != null)
                            {
                                if (((Interface)packageableElement).RedefinedInterfacesRefs == null)
                                {
                                    ((Interface)packageableElement).RedefinedInterfacesRefs = new List<string>();
                                }

                                ((Interface)packageableElement).RedefinedInterfacesRefs!.Add(baseTypeElement.FullName);
                            }
                        }

                        

                        AddProperties(package, packageableElement, type, repository);
                    }
                    else if (type.IsEnum)
                    {
                        packageableElement = new Enumeration
                        {
                            Name = type.Name,
                            OwningPackage = package,

                        };

                        package.PackagedElements.Add(packageableElement);

                        Array enumValues = type.GetEnumValues();

                        foreach (object? enumValue in enumValues)
                        {
                            ((Enumeration)packageableElement).OwnedLiterals.Add(new EnumerationLiteral
                            {
                                Name = enumValue.ToString(),
                                OwningPackage = package
                            });
                        }
                    }

                }

                result = packageableElement;
            }


            return result;
        }

        private Type[] GetDirectInterfaces(Type type)
        {
            // Alle Interfaces der Klasse
            Type[] allInterfaces = type.GetInterfaces();

            // Interfaces der Basisklasse (falls vorhanden)
            Type[] baseInterfaces = type.BaseType != null
                ? type.BaseType.GetInterfaces()
                : Array.Empty<Type>();

            // Nur direkt implementierte Interfaces
            IEnumerable<Type> directInterfaces = allInterfaces.Except(baseInterfaces);

            return directInterfaces.ToArray();
        }

        private void SetTemplateSettings(Classifier classifier, Type type, EmofRepository repository)
        {
            classifier.OwnedTemplateSignature = new TemplateSignature();

            Type[] genericTypeTypes = type.GetGenericArguments();

            foreach (Type genericTypeType in genericTypeTypes)
            {

                classifier.OwnedTemplateSignature.OwnedParameters.Add(new TemplateParameter
                {
                    DefaultTypeRef = genericTypeType.Name
                });

            }

        }

        private void AddProperties(Package package, 
                                   PackageableElement packageableElement, 
                                   Type type, 
                                   EmofRepository repository)
        {
            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.DeclaredOnly |
                                                              BindingFlags.Instance |
                                                              BindingFlags.Public);

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                bool isTypeReference = false;

                Type typeOfProperty = propertyInfo.PropertyType;

                Type typeForMof = typeOfProperty;

                TypeReferenceToAttribute[] typeReferenceToAttributes = (TypeReferenceToAttribute[])Attribute.GetCustomAttributes(propertyInfo, 
                                                                                                                           typeof(TypeReferenceToAttribute)
                                                                                                                           );
                if (typeReferenceToAttributes.Length == 1)
                {
                    isTypeReference = true;
                }

                string multiplicity = "1";

                PackageableElement? genericCollectionType = null;

                if (typeOfProperty.Name.StartsWith("List") ||
                    typeOfProperty.Name.StartsWith("ObservableCollection") ||
                    typeOfProperty.Name.StartsWith("Dictionary") || typeOfProperty.IsArray ||
                   propertyInfo.GetIndexParameters().Length > 0)
                {
                    multiplicity = "*";

                    if (typeOfProperty.Name.StartsWith("List"))
                    {
                        typeForMof = typeOfProperty.GetGenericArguments()[0];

                        genericCollectionType = GetOrCreateElementRecursively(typeOfProperty, repository);
                    }
                }

                PackageableElement? propertyTypeElement = GetOrCreateElementRecursively(typeForMof, repository);

                string propertyTypeRef = "";

                if (propertyTypeElement != null)
                {
                    propertyTypeRef = propertyTypeElement.FullName;
                }

                Property property = new Property()
                {
                    Name = propertyInfo.Name,
                    TypeRef = propertyTypeRef,
                    Kind = PropertyKind.Property,
                    IsReadOnly = !propertyInfo.CanWrite,
                    Multiplicity = multiplicity
                };
                if (genericCollectionType != null)
                {
                    property.CollectionTypeRef = genericCollectionType.FullName;
                }

                AddPropertyAnnotations(propertyInfo, property, repository);

                if (!isTypeReference)
                {
                    if (type.IsClass && packageableElement != null)
                    {
                        Class emofClass = (Class)packageableElement;
                        emofClass.OwnedAttributes.Add(property);
                    }
                    else if (type.IsInterface && packageableElement != null)
                    {
                        Interface emofInterface = (Interface)packageableElement;
                        emofInterface.OwnedAttributes.Add(property);
                    }
                }
                else // create association
                {

                    Type referencedType = typeReferenceToAttributes[0].Type;

                    if (packageableElement != null)
                    {
                        Association association = new Association()
                        {
                            OwningPackage = package,
                            Name = propertyInfo.Name
                        };

                        Property assocationSource = new Property()
                        {
                            TypeRef = packageableElement.FullName,
                            Name = string.Empty,
                            Multiplicity = "1"
                        };

                        association.OwnedEnds.Add(assocationSource);

                        property.TypeRef = referencedType.FullName;

                        association.OwnedEnds.Add(property);

                        package.PackagedElements.Add(association);
                    }
                }
            }
        }

        private void AddPropertyAnnotations(PropertyInfo propertyInfo, Property property, EmofRepository repository)
        {
            IEnumerable<Attribute> customAttributes = propertyInfo.GetCustomAttributes();
            
            //IList<CustomAttributeData> attributeDatas = CustomAttributeData.GetCustomAttributes(propertyInfo);

            //foreach(CustomAttributeData customAttributeData in attributeDatas)
            //{
            //    IList<CustomAttributeTypedArgument> constructorArguments = customAttributeData.ConstructorArguments;
            //    var namedArgument = customAttributeData.NamedArguments;

            //    ConstructorInfo constructor = customAttributeData.Constructor;
            //    ParameterInfo[] parameterInfos = constructor.GetParameters();
            //}


            foreach(Attribute attribute in customAttributes)
            {
                Type attributeType = attribute.GetType();

                if (attributeType.Namespace != "MDD4All.DataModeling.Attributes")
                {

                    PackageableElement? attributeTypeElement = GetOrCreateElementRecursively(attributeType, repository);

                    if (attributeTypeElement != null)
                    {
                        InstanceSpecification annotationInstance = new InstanceSpecification();
                        annotationInstance.ClassifierRef = attributeType.FullName;

                        PropertyInfo[] propertyInfos = attributeType.GetProperties(BindingFlags.DeclaredOnly |
                                                                                   BindingFlags.Instance |
                                                                                   BindingFlags.Public);

                        foreach (PropertyInfo attributePropertyInfo in propertyInfos)
                        {
                            object? value = attributePropertyInfo.GetValue(attribute);

                            if (value != null)
                            {
                                Slot slot = new Slot
                                {
                                    DefiningFeatureRef = attributePropertyInfo.Name
                                };

                                slot.Value = value.ToString();

                                if (annotationInstance.Slots == null)
                                {
                                    annotationInstance.Slots = new List<Slot>();
                                }
                                annotationInstance.Slots.Add(slot);
                            }


                        }

                        if (property.Annotations == null)
                        {
                            property.Annotations = new List<InstanceSpecification>();
                        }
                        property.Annotations.Add(annotationInstance);
                    }
                }
            }
        }

        private Package? GetOrCreatePackageForNamespace(string namespaceName, EmofRepository repository)
        {
            Package? result = null;

            string[] namepsaceParts = namespaceName.Split('.');

            if (namepsaceParts.Length > 0)
            {
                result = GetOrCreateChildPackage(repository.RootPackages, namepsaceParts[0], null);

                if (result != null)
                {
                    for (int counter = 1; counter < namepsaceParts.Length; counter++)
                    {
                        result = GetOrCreateChildPackage(result!.NestedPackages, namepsaceParts[counter], result);
                    }
                }
            }
            return result;

        }

        private Package? GetOrCreateChildPackage(List<Package> children, string childName, Package? parent)
        {
            Package? result = null;

            foreach (Package childPackage in children)
            {
                if (childPackage.Name == childName)
                {
                    result = childPackage;
                    break;
                }
            }

            // if not found, create one
            if (result == null)
            {
                result = new Package()
                {
                    Name = childName
                };

                result.OwningPackage = parent;

                children.Add(result);
            }

            return result;
        }



    }
}

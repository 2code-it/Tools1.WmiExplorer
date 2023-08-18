using System.Management;
using System.Security;
using static System.Formats.Asn1.AsnWriter;

namespace Tools1.WmiMeta
{
	public class WmiMetaService : IWmiMetaService
	{
		public WmiMetaService() : this(_defaultHostName) { }
		public WmiMetaService(string hostName) : this(hostName, false, null, null) { }
		public WmiMetaService(string hostName, bool enablePrivileges, string? logonUserName, SecureString? logonUserPasword)
		{
			HostName = hostName;
			LogonUserName = logonUserName;
			LogonUserPassword = logonUserPasword;
			EnablePrivileges = enablePrivileges;
		}

		const string _rootNamespace = "root";
		const string _defaultHostName = "localhost";
		const string _namespacePath = "__namespace";
		const string _classPath = "__class";


		private readonly ObjectGetOptions _objectGetOptions = new ObjectGetOptions { UseAmendedQualifiers = true };
		private readonly System.Management.EnumerationOptions _enumerationOptions = new System.Management.EnumerationOptions() { EnumerateDeep = true };


		public string HostName { get; set; }
		public string? LogonUserName { get; set; }
		public SecureString? LogonUserPassword { get; set; }
		public bool EnablePrivileges { get; set; }

		public event EventHandler<UnhandledExceptionEventArgs>? Error;

		public WmiMetaObject[] GetRootNamespaces()
			=> GetNamespaces(_rootNamespace);

		public WmiMetaObject[] GetNamespaces(string namespacePath)
		{
			Func<WmiMetaObject[]> getter = () =>
			{
				ManagementScope scope = GetConnectedManagementScope(namespacePath);
				return GetManagementClassInstances(scope, _namespacePath, _objectGetOptions)
					.Select(x => GetWmiMetaObjectForNamespace(x, namespacePath)).ToArray();
			};

			return TryGet(getter, $"Failed to get namespaces for parent {namespacePath}", new WmiMetaObject[0]);
		}

		public WmiMetaObject[] GetClasses(string namespacePath)
		{
			Func<WmiMetaObject[]> getter = () =>
			{
				ManagementScope scope = GetConnectedManagementScope(namespacePath);
				string[] classNames = GetManagamentClassSubClasses(scope, _objectGetOptions, _enumerationOptions)
						.Select(x => (string)x[_classPath]).ToArray();
				ManagementClass[] classObjects = classNames.Select(x => TryGetClassByName(scope, x)).Where(x => x is not null).ToArray()!;
				return classObjects.Select(x => GetWmiMetaObjectForClass(x)).ToArray();
			};

			return TryGet(getter, $"Failed to get subclasses for namespace {namespacePath}", new WmiMetaObject[0]);
		}


		public WmiMetaObject[] GetProperties(string namespacePath, string className)
		{
			Func<WmiMetaObject[]> getter = () =>
			{
				ManagementScope scope = GetConnectedManagementScope(namespacePath);
				string fullNamespace = $"{namespacePath}\\{className}";
				return GetManagamentClassProperties(scope, className, _objectGetOptions)
					.Select(x => GetWmiMetaObjectForProperty(x, fullNamespace)).ToArray();
			};

			return TryGet(getter, $"Failed to get properties class {namespacePath}\\{className}", new WmiMetaObject[0]);
		}

		private T TryGet<T>(Func<T> getter, string errorMessage, T errorValue)
		{
			try
			{
				return getter.Invoke();
			}
			catch (Exception ex)
			{
				OnError(new InvalidOperationException(errorMessage, ex));
				return errorValue;
			}
		}

		private ManagementClass? TryGetClassByName(ManagementScope scope, string name)
		{
			return TryGet(() => GetManagementClass(scope, name, _objectGetOptions), $"Failed to load class {name}", null);
		}

		private WmiMetaObject GetWmiMetaObjectForNamespace(ManagementObject managementObject, string ns)
		{
			WmiMetaObject metaObject = new WmiMetaObject();
			metaObject.Path = managementObject.Path.ToString();
			metaObject.Name = $"{ns}\\{managementObject["Name"]}";
			metaObject.Description = GetDescriptionTextFromQualifiers(managementObject.Qualifiers);
			return metaObject;
		}

		private WmiMetaObject GetWmiMetaObjectForClass(ManagementObject managementObject)
		{
			WmiMetaObject metaObject = new WmiMetaObject();
			metaObject.Path = managementObject.Path.ToString();
			metaObject.Name = $"{managementObject[_classPath]}";
			metaObject.Description = GetDescriptionTextFromQualifiers(managementObject.Qualifiers);
			return metaObject;
		}

		private WmiMetaObject GetWmiMetaObjectForProperty(PropertyData property, string ns)
		{
			WmiMetaObject metaObject = new WmiMetaObject();
			metaObject.Path = $"{ns}\\{property.Name}";
			metaObject.Name = property.Name;
			metaObject.Description = GetDescriptionTextFromQualifiers(property.Qualifiers);
			return metaObject;
		}

		private ManagementScope GetConnectedManagementScope(string namespaceName)
		{
			ManagementScope scope = new ManagementScope($"\\\\{HostName}\\{namespaceName}");
			scope.Options.EnablePrivileges = EnablePrivileges;

			if (LogonUserName is not null && LogonUserPassword is not null)
			{
				scope.Options.Username = LogonUserName;
				scope.Options.SecurePassword = LogonUserPassword;
			}

			scope.Connect();
			return scope;
		}

		private ManagementClass GetManagementClass(ManagementScope scope, string path, ObjectGetOptions getOptions)
			=> new ManagementClass(scope, new ManagementPath(path), getOptions);

		private ManagementClass GetManagementClass(ManagementScope scope, ObjectGetOptions getOptions)
			=> new ManagementClass(scope, new ManagementPath(), getOptions);

		private ManagementObject[] GetManagementClassInstances(ManagementScope scope, string path, ObjectGetOptions getOptions)
			=> GetManagementClass(scope, path, getOptions).GetInstances().Cast<ManagementObject>().ToArray();

		private ManagementObject[] GetManagamentClassSubClasses(ManagementScope scope, ObjectGetOptions getOptions, System.Management.EnumerationOptions enumerationOptions)
			=> GetManagementClass(scope, getOptions).GetSubclasses(enumerationOptions).Cast<ManagementObject>().ToArray();

		private PropertyData[] GetManagamentClassProperties(ManagementScope scope, string path, ObjectGetOptions getOptions)
			=> GetManagementClass(scope, path, getOptions).Properties.Cast<PropertyData>().ToArray();

		private string? GetDescriptionTextFromQualifiers(QualifierDataCollection qualifiers)
		{
			QualifierData? qualifierDescription = qualifiers.Cast<QualifierData>().FirstOrDefault(x => x.Name == "Description");
			return GetValueAsString(qualifierDescription?.Value);
		}

		private string? GetValueAsString(object? value)
		{
			if (value is null) return null;
			if (value.GetType().IsArray) return GetArrayAsString((Array)value);
			return Convert.ToString(value);
		}

		private string GetArrayAsString(Array value)
		{
			Array values = value;
			string?[] stringValues = new string[values.Length];
			for (var i = 0; i < values.Length; i++)
			{
				stringValues[i] = Convert.ToString(values.GetValue(i));
			}
			return string.Join(',', stringValues);
		}

		protected virtual void OnError(Exception exception)
		{
			if (Error is null) throw exception;
			Error.Invoke(this, new UnhandledExceptionEventArgs(exception, false));
		}
	}
}

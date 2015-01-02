<#
this.ContributeBaseTypes += (templateType, baseTypes) => {
	if (templateType.IsRecursiveParent) {
		baseTypes.Add("IRecursiveParentWithLookupTable<" + templateType.RecursiveType.TypeName + ">");
		baseTypes.Add("IRecursiveParentWithChildReplacement<" + templateType.RecursiveType.TypeName + ">");
	}
};

this.HandleType += (templateType, position) => {
	switch (position) {
		case HandleTypePosition.InsideConstructor:
			if (templateType.IsRecursive) {
#>
		var lookupTableInitResult = RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName #>, <#= templateType.RecursiveParent.TypeName #>>.Initialize(this, <#= templateType.LookupTableField.NameCamelCase #>);
		this.lookupTable = lookupTableInitResult.LookupTable;
		this.inefficiencyLoad = lookupTableInitResult.InefficiencyLoad;
<#
			}
			
			break;

		case HandleTypePosition.WithinTypeDeclaration:
			if (templateType.IsRecursiveType) {
				var recursiveType = templateType.RecursiveTypeFromFamily;
			}

			if (templateType.IsRecursive) {
#>

private <#= templateType.NodeLookupTableTypeName #> lookupTable;

private uint inefficiencyLoad;

private <#= templateType.NodeLookupTableTypeName #> LookupTable {
	get {
		if (this.lookupTable == RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName #>, <#= templateType.RecursiveParent.TypeName #>>.LazySentinel) {
			this.lookupTable = RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName #>, <#= templateType.RecursiveParent.TypeName #>>.CreateLookupTable(this);
		}

		return this.lookupTable;
	}
}

uint IRecursiveParentWithLookupTable<<#= templateType.RecursiveType.TypeName #>>.InefficiencyLoad {
	get { return this.inefficiencyLoad; }
}

System.Collections.Generic.IReadOnlyCollection<<#= templateType.RecursiveType.TypeName #>> IRecursiveParentWithLookupTable<<#= templateType.RecursiveType.TypeName #>>.Children {
	get { return this.<#= templateType.RecursiveField.NamePascalCase#>; }
}

System.Collections.Immutable.ImmutableDictionary<<#= templateType.RequiredIdentityField.TypeName #>, System.Collections.Generic.KeyValuePair<<#= templateType.RecursiveType.TypeName #>, <#= templateType.RequiredIdentityField.TypeName #>>> IRecursiveParentWithLookupTable<<#= templateType.RecursiveType.TypeName #>>.LookupTable {
	get { return this.LookupTable; }
}

public System.Collections.Immutable.ImmutableStack<<#= templateType.RecursiveType.TypeName #>> GetSpine(<#= templateType.RecursiveType.TypeName #> descendent) {
	return this.GetSpine<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>>(descendent);
}

public System.Collections.Immutable.ImmutableStack<<#= templateType.RecursiveType.TypeName #>> GetSpine(<#= templateType.RequiredIdentityField.TypeName #> identity) {
	return this.GetSpine<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>>(identity);
}

public <#= templateType.RecursiveType.TypeName #> Find(<#= templateType.RequiredIdentityField.TypeName #> identity)
{
	return this.Find<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>>(identity);
}

public <#= templateType.RecursiveParent.TypeName #> GetParent(<#= templateType.RecursiveType.TypeName #> descendent) {
	return this.GetParent<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>>(descendent);
}

public ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>> GetParentedNode(<#= templateType.RequiredIdentityField.TypeName #> identity) {
	return this.GetParentedNode<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveType.TypeName #>>(identity);
}

IRecursiveParent<<#= templateType.RecursiveType.TypeName #>> IRecursiveParentWithChildReplacement<<#= templateType.RecursiveType.TypeName#>>.ReplaceChild(System.Collections.Immutable.ImmutableStack<<#= templateType.RecursiveType.TypeName #>> oldSpine, System.Collections.Immutable.ImmutableStack<<#= templateType.RecursiveType.TypeName #>> newSpine) {
	var newChildren = this.Children.Replace(oldSpine.Peek(), newSpine.Peek());
	var newSelf = this.WithChildren(newChildren);

	var lookupTableLazySentinel = RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName#>, <#= templateType.RecursiveParent.TypeName#>>.LazySentinel;
	if (newSelf.lookupTable == lookupTableLazySentinel && this.lookupTable != null && this.lookupTable != lookupTableLazySentinel) {
		// Our newly mutated self wants a lookup table. If we already have one we can use it,
		// but it needs to be fixed up given the newly rewritten spine through our descendents.
		newSelf.lookupTable = RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName#>, <#= templateType.RecursiveParent.TypeName#>>.Fixup(this, ImmutableDeque.Create(newSpine), ImmutableDeque.Create(oldSpine));
		RecursiveTypeExtensions.LookupTable<<#= templateType.RecursiveType.TypeName#>, <#= templateType.RecursiveParent.TypeName#>>.ValidateInternalIntegrityDebugOnly(newSelf);
	}

	return newSelf;
}

<#
			}

			break;
		default:
			break;
	}
}; // event handler
#>
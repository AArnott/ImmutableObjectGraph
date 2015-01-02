<#
this.HandleType += (templateType, position) => {
	if (position != HandleTypePosition.WithinTypeDeclaration) {
		return;
	}

	if (templateType.IsRecursive) {
#>

public <#= templateType.TypeName #> AddDescendent(<#= templateType.RecursiveType.TypeName #> value, <#= templateType.RecursiveParent.TypeName #> parent) {
	var spine = this.GetSpine(parent);
	var newParent = parent.Add<#= templateType.RecursiveField.NamePascalCase #>(value);
	var newSpine = System.Collections.Immutable.ImmutableStack.Create(value, newParent);
	return (<#= templateType.TypeName #>)this.ReplaceDescendent(spine, newSpine, spineIncludesDeletedElement: false).Peek();
}

public <#= templateType.TypeName #> RemoveDescendent(<#= templateType.RecursiveType.TypeName #> value) {
	var spine = this.GetSpine(value);
	var spineList = spine.ToList();
	var parent = (<#= templateType.RecursiveParent.TypeName #>)spineList[spineList.Count - 2];
	var newParent = parent.Remove<#= templateType.RecursiveField.NamePascalCase #>(spineList[spineList.Count - 1]);

	var newSpine = System.Collections.Immutable.ImmutableStack.Create((<#= templateType.RecursiveType.TypeName #>)newParent);
	return (<#= templateType.RecursiveParent.TypeName #>)this.ReplaceDescendent(spine, newSpine, spineIncludesDeletedElement: true).Peek();
}

/// <summary>Replaces one node with a modified version of itself (same identity) among this node's descendents</summary>
public <#= templateType.TypeName #> ReplaceDescendent(<#= templateType.RecursiveType.TypeName #> updatedNode) {
	var spine = this.GetSpine(updatedNode.Identity);

	if (spine.IsEmpty) {
		// The descendent was not found.
		throw new System.ArgumentException("Old value not found");
	}

	return (<#= templateType.TypeName #>)this.ReplaceDescendent(spine, System.Collections.Immutable.ImmutableStack.Create(updatedNode), spineIncludesDeletedElement: false).Peek();
}

/// <summary>Replaces one node with another node that may have a different identity.</summary>
public <#= templateType.TypeName #> ReplaceDescendent(<#= templateType.RecursiveType.TypeName #> current, <#= templateType.RecursiveType.TypeName #> replacement) {
	var spine = this.GetSpine(current);

	if (spine.IsEmpty) {
		// The descendent was not found.
		throw new System.ArgumentException("Old value not found");
	}

	return (<#= templateType.TypeName #>)this.ReplaceDescendent(spine, System.Collections.Immutable.ImmutableStack.Create(replacement), spineIncludesDeletedElement: false).Peek();
}

<#
	}
}; // event handler
#>
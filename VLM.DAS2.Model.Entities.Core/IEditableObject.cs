namespace VLM.DAS2.Model.Entities.Core
{
    public interface IEditableObject
    {
        // Summary:
        //     Begins an edit on an object.
        void BeginEdit();
        //
        // Summary:
        //     Discards changes since the last System.ComponentModel.IEditableObject.BeginEdit()
        //     call.
        void CancelEdit();
        //
        // Summary:
        //     Pushes changes since the last System.ComponentModel.IEditableObject.BeginEdit()
        //     or System.ComponentModel.IBindingList.AddNew call into the underlying object.
        void EndEdit();
    }
}

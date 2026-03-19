/// <summary>
/// 세이브 백엔드 공통 상수. Firestore, Local 등에서 사용.
/// </summary>
public static class SaveConstants
{
    public static class Firestore
    {
        public const string CollectionUsers = "users";
        public const string CollectionSave = "save";
        public const string DocumentSlot = "save_slot0";
        public const string FieldData = "data";
    }

    public static class Local
    {
        public const string FileName = "save_0.json";
    }
}

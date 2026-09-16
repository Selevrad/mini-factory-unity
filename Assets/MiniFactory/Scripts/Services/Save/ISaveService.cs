using MiniFactory.Domain;

namespace MiniFactory.Services.Save
{
    public interface ISaveService
    {
        FactorySaveData Load();
        void Save(FactorySaveData data);
    }
}

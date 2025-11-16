namespace LDtkMonogameExample;

using Microsoft.Xna.Framework;

interface IMonogame
{
    void Initialize();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}

#include "game.hpp"
#include "move.hpp"
#include "keyboard.hpp"

int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR arg, int) {

	//dxlibの初期化
	SetGraphMode(960, 720, 32);
	SetBackgroundColor(255, 255, 255);
	ChangeWindowMode(TRUE);
	DxLib_Init();
	SetDrawScreen(DX_SCREEN_BACK);

	//ゲームの初期化
	Game game;
	game.init();

	while (ScreenFlip() == 0 && ProcessMessage() == 0 &&
		ClearDrawScreen() == 0 && keyboardUpdate() == 0) {
		game.update();
		if (game.getIsInEnd() == true) {
			break;
		}
		game.draw();
	}

	DxLib_End();
	return 0;
}
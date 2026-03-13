#include "pch.h"

char* cvt_str_cstr(winrt::hstring str)
{
	if (str.empty()) return NULL;
	const wchar_t* wstr = str.c_str();
	int wlen = str.size();
	char* data = (char*)malloc((wlen + 1) * 2);
	int len = ::WideCharToMultiByte(CP_ACP, 0, wstr, wlen, data, (wlen + 1) * 2, NULL, NULL);
	data[len] = 0;
	return data;
}

winrt::hstring cvt_cstr_str(const char* str)
{
	if (!str || !str[0]) return L"";
	int len1 = strlen(str) + 2;
	wchar_t* wtxt = (wchar_t*)malloc(sizeof(wchar_t) * len1);
	int len = MultiByteToWideChar(CP_ACP, 0, str, -1, wtxt, len1);
	wtxt[len] = 0;
	winrt::hstring ret = wtxt;
	free(wtxt);
	return ret;
}

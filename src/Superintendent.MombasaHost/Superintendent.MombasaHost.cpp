// Superintendent.MombasaHost.cpp : This file contains the 'main' function. Program execution begins and ends there.
//
#define _SILENCE_STDEXT_ARR_ITERS_DEPRECATION_WARNING

#include <iostream>
#include <string>
#include <chrono>
#include <thread>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_color_sinks.h"

int work1() {
    std::cout << "Work1" << std::endl;
    return 69;
}

int work2(int a, int b, int c, int d, int e) {
    std::cout << "Work2 " << a << " " << b << " " << c << " " << d << " " << e << " " << std::endl;
    return 72;
}

int thrower(int a, int b, int c, int d, int e) {
    throw std::exception("This is an exception message", 42069);
}


std::string secret = "thisissecretyo";

int main()
{
    auto console = spdlog::stdout_color_mt("console");
    auto err_logger = spdlog::stderr_color_mt("stderr");

    std::cout << "Text: " << (void*)secret.c_str() << std::endl;
    std::cout << "Work1: " << (void*)&work1 << std::endl;
    std::cout << "Work2: " << (void*)&work2 << std::endl;
    std::cout << "Thrower: " << (void*)&thrower << std::endl;

    auto mombasa = LoadLibrary(L"mombasa.dll");

    std::string line;
    std::cin >> line;
    std::cout << line;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file

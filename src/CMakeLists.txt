find_package(Qt4 REQUIRED)
find_package(Smoke REQUIRED)

set(assemblygen_native_SRC
    assemblygen-native.cpp
)

include_directories(${QT_INCLUDES} "${SMOKE_INCLUDE_DIR}")
add_library(assemblygen-native SHARED ${assemblygen_native_SRC})
target_link_libraries(assemblygen-native ${QT_QTCORE_LIBRARY} ${SMOKE_BASE_LIBRARY})

set(assemblygen_SRC
    AttributeGenerator.cs
    ByteArrayManager.cs
    ClassesGenerator.cs
    ClassInterfacesGenerator.cs
    CodeDomExtensions.cs
    EnumGenerator.cs
    GeneratorData.cs
	HtmlEncoder.cs
    main.cs
    MethodsGenerator.cs
    PluginInterfaces.cs
    Smoke.cs
    SmokeMethodEqualityComparer.cs
    SmokeMethods.cs
    SmokeSupport.cs
    Translator.cs
    Util.cs
)

if (WIN32)
    # Build assemblygen as an x86 application, so that it can
    # dynamically load smokeqt DLLs.
    csharp_add_executable(assemblygen ${assemblygen_SRC}
		COMPILE_FLAGS /platform:x86 UNSAFE COMPILE_DEFINITIONS DEBUG)
else (WIN32)
    csharp_add_executable(assemblygen ${assemblygen_SRC} UNSAFE
        COMPILE_DEFINITIONS DEBUG)
endif (WIN32)

if (MONO_EXECUTABLE)
    configure_file(assemblygen.cmake assemblygen @ONLY)
    install(FILES ${CMAKE_CURRENT_BINARY_DIR}/assemblygen DESTINATION bin
            PERMISSIONS OWNER_WRITE OWNER_READ OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE)
    install_assembly(NO_GAC assemblygen DESTINATION lib${LIB_SUFFIX}/assemblygen)
else (MONO_EXECUTABLE)
    install_assembly(NO_GAC assemblygen DESTINATION bin)
endif (MONO_EXECUTABLE)

install(TARGETS assemblygen-native DESTINATION lib${LIB_SUFFIX}/assemblygen)
if (WIN32)
    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(assemblygen-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)

add_subdirectory(plugins)

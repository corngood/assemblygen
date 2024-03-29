set (qyoto_qtscript_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/assemblygen.exe
	-unsafe -out:qyoto-qtscript.dll -r:qyoto-qtcore.dll -namespace:QtScript
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtscript.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtscript${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtscript_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
	DEPENDS ${qyoto_qtscript_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtscript ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll)
add_dependencies(qyoto-qtscript assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtscript PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll)
set_property(TARGET qyoto-qtscript PROPERTY _assembly_type dll)



install_assembly(qyoto-qtscript DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.xml DESTINATION share/qyoto)

add_subdirectory(native)
